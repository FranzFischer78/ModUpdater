﻿using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ModUpdater : Mod
{

	public static string Temppath = Path.GetTempPath() + @"\RaftModUpdaterTemp";
	public static string WWWResult;
	public static ModUpdater instanceMod;

	public static HNotification notification;

	public static bool Autoupdate;

	public static string UnofficialFixes;
	public static string OutdatedMods;
	public static string OutdatedModsWithAlts;

	public static bool ExtraSettingsAPI_Loaded = false; // This is set to true while the mod's settings are loaded

	public static bool SettingLoaded = false;

	static AssetBundle asset;


	private const string id = "franzfischer78.modupdater";
	private Harmony harmony = null;

	public static bool DidAtLeastOneUpdate = false;


	public static List<ModCache> modCache = new List<ModCache>();

	GameObject disclaimerWin;


	public async void Start()
	{
		notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, "Loading ModUpdater...");
		//HNotification notification7 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Disclaimer: The Modupdater accesses your filesystem. Although it is unlikely to happen, me and the Raftmodding Team are not responsible for any damage caused to your computer by the modupdater!", 10, HNotify.ErrorSprite);
		AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("modupdatedlbtn.assets"));
		await request;
		asset = request.assetBundle;


		if (PlayerPrefs.HasKey("ModUpdaterAccept"))
		{
			Debug.Log("The key exists");
		}
		else
		{
			disclaimerWin = asset.LoadAsset<GameObject>("DisclaimerWin");
			Instantiate(disclaimerWin);
			disclaimerWin.transform.Find("AcceptDisclaim").GetComponent<Button>().onClick.AddListener(AcceptDisc);
			disclaimerWin.transform.Find("DeclineDisclaim").GetComponent<Button>().onClick.AddListener(DeclineDisc);
		}
		 
		instanceMod = this;
		Autoupdate = false;

		modCache.Clear();

		Debug.Log("[Modupdater] Loading unofficial fixes");
		Task GetFixes = (Task)GetUnofficialFixes("https://raw.githubusercontent.com/FranzFischer78/ModUpdater/master/Databases/unofficialfix.json", "unofficial");
		await GetFixes;
		GetFixes = (Task)GetUnofficialFixes("https://raw.githubusercontent.com/FranzFischer78/ModUpdater/master/Databases/outdated.json", "outdated");
		await GetFixes;
		GetFixes = (Task)GetUnofficialFixes("https://raw.githubusercontent.com/FranzFischer78/ModUpdater/master/Databases/outdatedalts.json", "outdatedAlts");
		await GetFixes;
		Debug.Log("ModUpdater has been loaded!");
		Task CheckForUpdatesTask = (Task)CheckForUpdates();
		await CheckForUpdatesTask;
		StartCoroutine(InitAutoUpdate());
		//Debug.Log("Autoupdate: " + Autoupdate);

		


		//(harmony = new Harmony(id)).PatchAll(System.Reflection.Assembly.GetExecutingAssembly());


		StartCoroutine(LoadUpdateButtons());
		StartCoroutine(WaitForNextModListRefresh());

		notification.Close();



	}

	public void AcceptDisc()
	{
		PlayerPrefs.SetString("ModUpdaterAccept", "true");
	}

	public void DeclineDisc()
	{
		System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(HMLLibrary.HLib.path_modsFolder));
		Application.Quit();
	}

	public IEnumerator LoadUpdateButtons()
	{
		//Trying to hook into the ui :grin: ;)
		//HMLLibrary.ModManagerPage.modList[1].modinfo.ModlistEntry -> Get the GameObject for the mod list entry
		var modlistGO = HMLLibrary.ModManagerPage.modList;




		int i = 0;
		foreach (var modlistEntry in modlistGO)
		{

			GameObject modlistEntryGO = modlistEntry.modinfo.ModlistEntry;

			float width = modlistEntryGO.GetComponent<RectTransform>().rect.width;
			float xpos = width * 0.75f;

			Text ColorCode = modlistEntry.modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>();

			if (ColorCode.color != HMLLibrary.ModManagerPage.orangeColor)
			{
				if (ColorCode.color == HMLLibrary.ModManagerPage.blueColor || ColorCode.color == HMLLibrary.ModManagerPage.redColor)
				{
					GameObject UpdateBTN = Instantiate(asset.LoadAsset<GameObject>("ModupdaterDLBTN"), modlistEntryGO.transform);
					//UpdateBTN.GetComponent<RectTransform>().rect. = xpos;
					Sprite dlicon = asset.LoadAsset<Sprite>("icongreenstyle");
					UpdateBTN.GetComponent<Image>().sprite = dlicon;
					string[] str = new string[] { HMLLibrary.ModManagerPage.modList[i].jsonmodinfo.name };
					UpdateBTN.GetComponent<Button>().onClick.AddListener(delegate { UpdateMod(str); });
				}
				else
				{
					/*GameObject UpdateBTN = Instantiate(asset.LoadAsset<GameObject>("ModupdaterDLBTN"), modlistEntryGO.transform);
					//UpdateBTN.GetComponent<RectTransform>().rect. = xpos;
					Debug.Log("LoadRed");
					Sprite dlicon = asset.LoadAsset<Sprite>("iconredstyle");
					UpdateBTN.GetComponent<Image>().sprite = dlicon;
					UpdateBTN.GetComponent<Button>().interactable = false;*/
				}
			}
			i++;

		}

		yield return null;
	}

	IEnumerator WaitForNextModListRefresh()
	{

		while (HMLLibrary.ModManagerPage.canRefreshModlist)
		{
			yield return new WaitForSeconds(0.01f);

		}
		while (!HMLLibrary.ModManagerPage.canRefreshModlist)
		{
			yield return new WaitForSeconds(0.01f);

		}

		Task x = (Task)AfterRefresh();
		yield return x;


	}

	async Task AfterRefresh()
	{
		//Debug.Log("AfterRefresh");
		HNotification notification4 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, "Loading ModUpdater...");

		Task CheckForUpdatesTask = (Task)CheckForUpdates();
		await CheckForUpdatesTask;
		StartCoroutine(LoadUpdateButtons());
		StartCoroutine(WaitForNextModListRefresh());
		notification4.Close();
	}





	IEnumerator InitAutoUpdate()
	{
		while (SettingLoaded == false)
		{
			yield return new WaitForSeconds(0.01f);
		}
		if (Autoupdate == true)
		{
			Debug.Log("[ModUpdater] AutoUpdate mods");
			string[] str = new string[] { };
			UpdateAllMods(str);
		}

	}
	public static void ExtraSettingsAPI_ButtonPress(string name) // Occurs when a settings button is clicked. "name" is set the the button's name
	{
		if (name == "updateAllButton")
		{
			string[] str = new string[] { };
			UpdateAllMods(str);

		}
	}

	public static bool ExtraSettingsAPI_GetCheckboxState(string SettingName) => false;

	public static void ExtraSettingsAPI_Load() // Occurs when the API loads the mod's settings
	{
		Debug.Log("[Modupdater] Extra settings found");
		//Debug.Log("Checkbox state: " + ExtraSettingsAPI_GetCheckboxState("autoUpdate"));
		Autoupdate = ExtraSettingsAPI_GetCheckboxState("autoUpdate");
		SettingLoaded = true;


	}


	//This is actually no more only for unofficial fixes. It's for getting all the Database stuff
	public static async Task GetUnofficialFixes(string url, string type)
	{

		UnityWebRequest uwrrrr = UnityWebRequest.Get(url);
		await uwrrrr.SendWebRequest();

		if (uwrrrr.isNetworkError)
		{
			//Debug.Log("Error While Sending: " + uwrrrr.error);
			Debug.Log("[Modupdater] Couldn't get unnoficial Fixes. Trying again!");
			Debug.Log("[Modupdater] Loading unofficial fixes...");
			Task GetFixes = (Task)GetUnofficialFixes(url, type);
			await GetFixes;

		}
		else
		{
			Debug.Log("[Modupdater] Got list of unofficial Fixes!");

			switch (type)
			{
				case "unofficial":
					UnofficialFixes = uwrrrr.downloadHandler.text;
					break;
				case "outdated":
					OutdatedMods = uwrrrr.downloadHandler.text;
					break;
				case "outdatedAlts":
					OutdatedModsWithAlts = uwrrrr.downloadHandler.text;
					break;

			}

			//Debug.Log(UnofficialFixes);
		}
	}

	public static async Task CheckForUpdates()
	{
		for (int i = 0; i < HMLLibrary.ModManagerPage.modList.ToArray().Length; i++)
		{
			Task UpdateTask = (Task)CheckForUpdateFunc(HMLLibrary.ModManagerPage.modList[i].jsonmodinfo.name);
			await UpdateTask;


		}
	}

	public void OnModUnload()
	{
		Debug.Log("ModUpdater has been unloaded!");
	}

	[ConsoleCommand(name: "UpdateAllMods", docs: "Updates all the installed mods to the newest version")]
	public static async void UpdateAllMods(string[] args)
	{
		HNotification notification3;

		Debug.Log("[Modupdater] Updating all mods!");
		notification3 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, "Updating all Mods...");
		DidAtLeastOneUpdate = false;

		for (int i = 0; i < HMLLibrary.ModManagerPage.modList.ToArray().Length; i++)
		{
			Task UpdateTask = (Task)UpdateModFunc(HMLLibrary.ModManagerPage.modList[i].jsonmodinfo.name, false);
			await UpdateTask;


		}
		notification3.Close();
		if (DidAtLeastOneUpdate)
		{
			notification3 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Finished Updating! Restart the Game!", 5, HNotify.CheckSprite);
		}
		else
		{
			//notification3 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "All of your mods are up to date!", 5, HNotify.CheckSprite);
		}
	}

	[ConsoleCommand(name: "UpdateMod", docs: "Updates a specific mod")]
	public static async void UpdateMod(string[] args)
	{
		Debug.Log(string.Join(" ", args));
		Task UpdateTask = (Task)UpdateModFunc(string.Join(" ", args), true);
		notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, "Updating Mods...");
		DidAtLeastOneUpdate = false;
		await UpdateTask;
		notification.Close();
		if (DidAtLeastOneUpdate)
		{
			notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Finished Updating! Restart the Game!", 5, HNotify.CheckSprite);
		}
		else
		{
			//notification = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "No new version is available!", 5, HNotify.CheckSprite);
		}
	}



	public static async Task CheckForUpdateFunc(string modname)
	{
		HNotification notification2;

		bool UnofficialFix = false;
		bool Outdated = false;
		string OutdatedAlt = "";

		string slug = "";

		bool NeedsUpdate = false;

		int index = 9999;


		Debug.Log("[Modupdater] Check for Updates for " + modname);



		List<ModData> md = HMLLibrary.ModManagerPage.modList;


		for (int i = 0; i < HMLLibrary.ModManagerPage.modList.ToArray().Length; i++)
		{
			if (HMLLibrary.ModManagerPage.modList[i].jsonmodinfo.name == modname)
			{
				index = i;
			}
		}

		if (index == 9999)
		{
			Debug.Log("[Modupdater] The mod failed to Update! Does it even exist?");
			//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "The mod failed to Update! Does it even exist?", 5, HNotify.ErrorSprite);
			HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Unknown";

			HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.orangeColor;



		}
		else
		{
			if (HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl.Contains("YOUR-MOD-SLUG-HERE"))
			{
				Debug.Log("[Modupdater] Mod not found on Raftmodding Server. Is it one of yours?");
				//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Mod not found on Raftmodding Server. Is it one of yours?", 5, HNotify.ErrorSprite);
				HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Unknown";
				HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.orangeColor;

			}
			else
			{


				UnityWebRequest uwr = UnityWebRequest.Get(HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl);
				await uwr.SendWebRequest();

				if (uwr.isNetworkError)
				{
					Debug.Log("[Modupdater] Error While Sending: " + uwr.error);
					Debug.Log("[Modupdater] Couldn't update mod: " + modname);
					//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Couldn't update mod: " + modname, 5, HNotify.ErrorSprite);
					HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Unknown";
					HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.orangeColor;
				}
				else
				{
					//Debug.Log("Received: " + uwr.downloadHandler.text);
					WWWResult = uwr.downloadHandler.text;
					//Debug.Log(WWWResult.ToString());

					if (WWWResult.ToString().ToLower().Contains("404"))
					{
						Debug.Log("[Modupdater] Mod not found on Raftmodding Server. Is it one of yours? If not it may be misconfigured. Update the mod manually and ping me and the author... Raft is not Shark Food and Lantern Physics got this issue.");
						//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Mod not found on Raftmodding Server. Is it one of yours? If not it may be misconfigured. Update the mod manually and ping me (FranzFischer#6710) and the author... Raft is not Shark Food got this issue." + modname, 5, HNotify.ErrorSprite);
						HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Unknown/Misconfigured";
						HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.orangeColor;
					}
					else
					{

						string LocalVersion = HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.version;
						string RemoteVersion = WWWResult.ToString();

						LocalVersion = LocalVersion.ToLower().Replace("v", "");
						RemoteVersion = RemoteVersion.ToLower().Replace("v", "");

						string LocalForSem = LocalVersion.Split(' ')[0];
						string RemoteForSem = RemoteVersion.Split(' ')[0];


						var localSemVersion = new SemVer(LocalForSem);
						var remoteSemVersion = new SemVer(RemoteForSem);

						slug = HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl.Split('/')[6];
						//Debug.Log(slug);


						//Unofficial fix check

						int UPATCHVersion = 0;

						if (UnofficialFixes.Contains(slug))
						{
							JObject JsonContent = JObject.Parse(UnofficialFixes);
							JArray item = (JArray)JsonContent["urls"];

							for (int i = 0; i < item.Count; i++)
							{


								string nameofitem = (string)item[i]["slug"];
								if (nameofitem == slug)
								{
									UPATCHVersion = Convert.ToInt32((string)item[i]["version"]);
									Debug.Log(UPATCHVersion);
									break;
								}
								else
								{

								}
							}
							int LocalUpatch = 0;
							if (LocalVersion.Contains("UPATCH".ToLower()) == true)
							{
								char[] separators = new char[] { '[', ']' };

								/*Debug.Log(LocalVersion.Split(separators)[0]);
								Debug.Log(LocalVersion.Split(separators)[1]);

								Debug.Log(LocalVersion.Split(separators)[1].Split('h')[1]);*/

								LocalUpatch = Convert.ToInt32(LocalVersion.Split(separators)[1].Split('h')[1]);

								if (UPATCHVersion != 0 && LocalUpatch != 0)
								{

									if (UPATCHVersion > LocalUpatch)
									{
										UnofficialFix = true;
									}
									else
									{
										UnofficialFix = false;
									}

								}

							}

							//To patch all old versions containing [unofficial]
							if (LocalVersion.Contains("[unofficial]".ToLower()) == true || (LocalVersion.Contains("[unofficial]".ToLower()) == false && LocalVersion.Contains("UPATCH".ToLower()) == false))
							{
								UnofficialFix = true;
							}

						}

						//Oudated check:
						if (OutdatedMods.Contains(slug))
						{
							Outdated = true;

							if (OutdatedModsWithAlts.Contains(slug))
							{

								JObject JsonContent = JObject.Parse(OutdatedModsWithAlts);
								JArray item = (JArray)JsonContent["outdatedmods"];

								for (int i = 0; i < item.Count; i++)
								{


									string nameofitem = (string)item[i]["slug"];
									if (nameofitem == slug)
									{
										OutdatedAlt = (string)item[i]["alt-slug"];
										break;
									}
								}
							}
						}




						//This is just for debugging purpose 
						Debug.Log(LocalVersion);

						//Debug.Log(LocalVersion.Contains("[UNOFFICIAL]".ToLower()));

						//(LocalVersion != RemoteVersion || UnofficialFix) && noNewUnofficial == false
						//Check for version
						if (remoteSemVersion > localSemVersion || UnofficialFix || Outdated)
						{
							NeedsUpdate = true;
							Debug.Log("[Modupdater] There is a new version for " + modname);
							notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "There is a new version for " + modname, 5, HNotify.CheckSprite);

							if (UnofficialFix == true)
							{
								HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Unofficial Fix available!";
								HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.blueColor;
							}
							else
							{
								if (Outdated)
								{
									if (!OutdatedAlt.IsNullOrEmpty())
									{
										HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "OUTDATED (Uprade available)";
										HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.redColor;
									}
									else
									{
										HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "OUTDATED (Uninstallation recommended)";
										HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.orangeColor;
									}
								}
								else
								{
									HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Update available";
									HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.redColor;
								}
							}
						}
						else
						{
							Debug.Log("[Modupdater] There is no new version for " + modname);
							//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "There is no new version for " + modname, 5, HNotify.ErrorSprite);
							HMLLibrary.ModManagerPage.modList[index].modinfo.versionTooltip.GetComponentInChildren<TMPro.TMP_Text>().text = "Up to date";
							HMLLibrary.ModManagerPage.modList[index].modinfo.ModlistEntry.transform.Find("ModVersionText").GetComponent<UnityEngine.UI.Text>().color = HMLLibrary.ModManagerPage.greenColor;

						}
					}
				}
			}
		}

		//Assign to ModsCache

		ModCache cacheTMP = new ModCache();

		cacheTMP.slug = slug;
		cacheTMP.NeedsUpdate = NeedsUpdate;
		cacheTMP.UnofficialFixAvailable = UnofficialFix;
		cacheTMP.IsOutdated = Outdated;
		cacheTMP.AltSlug = OutdatedAlt;

		modCache.Add(cacheTMP);



	}


	public static async Task UpdateModFunc(string modname, bool OneModOnly)
	{
		HNotification notification2;


		string slug;

		Directory.CreateDirectory(Temppath);

		int index = 9999;
		int cacheIndex = 9999;

		Debug.Log("[Modupdater] Update mod " + modname);



		//Debug.Log("Try index assert");


		for (int i = 0; i < HMLLibrary.ModManagerPage.modList.ToArray().Length; i++)
		{
			if (HMLLibrary.ModManagerPage.modList[i].jsonmodinfo.name == modname)
			{
				index = i;
			}



		}

		if (index == 9999)
		{
			Debug.Log("[Modupdater] The mod failed to Update! Does it even exist?");
			//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "The mod failed to Update! Does it even exist?", 5, HNotify.ErrorSprite);

		}
		else
		{

			//Debug.Log("Passed index assert");

			slug = HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl.Split('/')[6];


			for (int i = 0; i < modCache.Count; i++)
			{
				//Debug.Log(modCache[i].slug);
				if (modCache[i].slug == slug)
				{
					cacheIndex = i;
					break;
				}
			}

			if (cacheIndex == 9999)
			{
				Debug.Log("[Modupdater] The mod failed to Update! Does it even exist?");
				//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "The mod failed to Update! Does it even exist?", 5, HNotify.ErrorSprite);

			}
			else
			{

				/*Debug.Log(cacheIndex);

				Debug.Log("Passed cache assert");

				Debug.Log(modCache[cacheIndex].NeedsUpdate);
				Debug.Log("Passed needs update");*/

				if (modCache[cacheIndex].NeedsUpdate)
				{
					DidAtLeastOneUpdate = true;

					//A newer version is available. Go ahead and download it
					Debug.Log("[Modupdater] Download new Version");
					string url = "";
					if (modCache[cacheIndex].UnofficialFixAvailable)
					{
						Debug.Log("[Modupdater] Download unofficial fix");
						JObject JsonContent = JObject.Parse(UnofficialFixes);
						JArray item = (JArray)JsonContent["urls"];

						for (int i = 0; i < item.Count; i++)
						{


							string nameofitem = (string)item[i]["slug"];
							if (nameofitem == slug)
							{
								url = (string)item[i]["discord-url"];
								break;
							}

						}
						if (url.IsNullOrEmpty())
						{
							Debug.Log("[Modupdater] Unexpected error downloading the unofficial fix");
							//url = "https://www.raftmodding.com/mods/" + slug + "/download?ignoreVirusScan=true";
						}


					}
					else if (modCache[cacheIndex].IsOutdated && !modCache[cacheIndex].AltSlug.IsNullOrEmpty())
					{
						Debug.Log("[Modupdater] Download alternative to Outdated Mod");
						JObject JsonContent = JObject.Parse(OutdatedModsWithAlts);
						JArray item = (JArray)JsonContent["outdatedmods"];

						for (int i = 0; i < item.Count; i++)
						{


							string nameofitem = (string)item[i]["slug"];
							if (nameofitem == slug)
							{
								url = "https://www.raftmodding.com/mods/" + (string)item[i]["alt-slug"] + "/download?ignoreVirusScan=true";
								break;
							}

						}
						if (url.IsNullOrEmpty())
						{
							Debug.Log("[Modupdater] Unexpected error while downloading the an outdated mod upgrade");
							//url = "https://www.raftmodding.com/mods/" + slug + "/download?ignoreVirusScan=true";
						}
					}
					else
					{
						//Donwload example url https://www.raftmodding.com/mods/itemspawner/download?ignoreVirusScan=true
						url = "https://www.raftmodding.com/mods/" + slug + "/download?ignoreVirusScan=true";
					}

					Debug.Log(url);

					if (!url.IsNullOrEmpty())
					{
						UnityWebRequest uwrr = new UnityWebRequest(url);
						uwrr.downloadHandler = new DownloadHandlerBuffer();
						Debug.Log("[Modupdater] Downloading...");
						await uwrr.SendWebRequest();



						if (uwrr.isNetworkError)
						{
							Debug.Log("[Modupdater] Error While Sending: " + uwrr.error);
							Debug.Log("[Modupdater] Couldn't update mod: " + modname + ". No bytes found!");
							//notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Couldn't update mod: " + modname + ". No bytes found!", 5, HNotify.ErrorSprite);


						}
						else
						{

							// retrieve results as binary data


							string filename = "";
							byte[] results = uwrr.downloadHandler.data;

							if (modCache[cacheIndex].IsOutdated && !modCache[cacheIndex].AltSlug.IsNullOrEmpty())
							{
								filename = "modinstaller." + modCache[cacheIndex].AltSlug + ".rmod";
							}
							else
							{

								filename = "modinstaller." + slug + ".rmod";

							}

							Debug.Log(filename);

							System.IO.File.WriteAllBytes(Temppath + @"\" + filename, results);

							//Clean up old mod
							Debug.Log("Debug modinfo.filename");
							Debug.Log(HMLLibrary.ModManagerPage.modList[index].modinfo.modFile.ToString());

							File.Delete(HMLLibrary.ModManagerPage.modList[index].modinfo.modFile.ToString());

							//Copy new one
							File.Copy(Temppath + @"\" + filename, @"mods\" + filename, true);
							Debug.Log("[Modupdater] Finished Downloading " + modname);
							notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "Finished Downloading " + modname, 5, HNotify.CheckSprite);


						}
					}
				}
				else
				{
					Debug.Log("[Modupdater] There is no new version for " + modname);
					if (OneModOnly)
					{
						notification2 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.normal, "There is no new version for " + modname, 5, HNotify.ErrorSprite);
					}

				}
			}
		}
	}

}



[Serializable]
public class ModCache
{

	public string slug;
	public bool NeedsUpdate;
	public bool UnofficialFixAvailable;
	public bool IsOutdated;
	public string AltSlug;

}






//Utility Functions

//Got this from: https://gist.github.com/krzys-h/9062552e33dd7bd7fe4a6c12db109a1a Credits going to this guy for saving my life XD
//I hate async functions XD

public class UnityWebRequestAwaiter : INotifyCompletion
{
	private UnityWebRequestAsyncOperation asyncOp;
	private Action continuation;

	public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
	{
		this.asyncOp = asyncOp;
		asyncOp.completed += OnRequestCompleted;
	}

	public bool IsCompleted { get { return asyncOp.isDone; } }

	public void GetResult() { }

	public void OnCompleted(Action continuation)
	{
		this.continuation = continuation;
	}

	private void OnRequestCompleted(AsyncOperation obj)
	{
		continuation();
	}
}

//This i created on my own and totally not based on the other one XD
public class UnityAssetBundleRequestAwaiter : INotifyCompletion
{
	private AssetBundleCreateRequest asyncOp;
	private Action continuation;

	public UnityAssetBundleRequestAwaiter(AssetBundleCreateRequest asyncOp)
	{
		this.asyncOp = asyncOp;
		asyncOp.completed += OnRequestCompleted;
	}

	public bool IsCompleted { get { return asyncOp.isDone; } }

	public void GetResult() { }

	public void OnCompleted(Action continuation)
	{
		this.continuation = continuation;
	}

	private void OnRequestCompleted(AsyncOperation obj)
	{
		continuation();
	}
}

public static class ExtensionMethods
{
	public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
	{
		return new UnityWebRequestAwaiter(asyncOp);
	}

	public static UnityAssetBundleRequestAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOp)
	{
		return new UnityAssetBundleRequestAwaiter(asyncOp);
	}
}
