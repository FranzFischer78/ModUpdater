//Put all that stuff in one function
		//instanceMod.StartCoroutine(instanceMod.getRequest(HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl, index, modname));


	}



	//Old coroutine mess code


	/*public static void GoOnWithUpdate(int index, string modname)
	{
        Debug.Log(WWWResult.ToString());

        string LocalVersion = HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.version;
        string RemoteVersion = WWWResult.ToString();

        LocalVersion = LocalVersion.ToLower().Replace("v","");
        RemoteVersion = RemoteVersion.ToLower().Replace("v","");

        if (LocalVersion != RemoteVersion)
        {
            //A newer version is available. Go ahead and download it
            Debug.Log("Download new Version");

            string slug = HMLLibrary.ModManagerPage.modList[index].jsonmodinfo.updateUrl.Split('/')[6];
            Debug.Log(slug);
            //Donwload example url https://www.raftmodding.com/mods/itemspawner/download?ignoreVirusScan=true
            string url = "https://www.raftmodding.com/mods/" + slug + "/download?ignoreVirusScan=true";

            instanceMod.StartCoroutine(instanceMod.getFileRequest(url, modname, slug));

        }
        else
        {
            Debug.Log("There is no new version for " + modname);
            return;
        }
    }

    IEnumerator getRequest(string uri, int index, string modname)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            Debug.Log("Couldn't update mod: " + modname);
            yield break;
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            WWWResult = uwr.downloadHandler.text;
            GoOnWithUpdate(index, modname);

        }

    }

    IEnumerator getFileRequest(string uri, string modname, string slug)
    {
        UnityWebRequest uwr = new UnityWebRequest(uri);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            Debug.Log("Couldn't update mod: " + modname);

            yield break;

        }
        else
        {
            // Show results as text
            Debug.Log("Downloading...");

            // Or retrieve results as binary data
            byte[] results = uwr.downloadHandler.data;

            string filename = "modinstaller." + slug + ".rmod";
            System.IO.File.WriteAllBytes(Temppath + @"\" + filename, results);
            File.Copy(Temppath + @"\" + filename, @"mods\" + filename, true);
            Debug.Log("Finished Downloading " + modname);
            Debug.Log("Updated Mod. Restart the game after you updated the mods you wanted!");

        }

    }*/