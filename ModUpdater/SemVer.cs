using System;

/*
 *  // https://semver.org/
    var webSiteSemVersion = new SemVer(websiteVersion);
    var modinfoSemVersion = new SemVer(modinfoVersion);

    newVersion = webSiteSemVersion > modinfoSemVersion;
 */

class SemVer : IComparable<SemVer>
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }

    public string Label { get; set; }

    public SemVer(string versionString)
    {
        var split = versionString.TrimStart('v').ToLower().Split('.');

        if (split.Length > 0)
        {
            if (int.TryParse(split[0], out var major))
            {
                this.Major = major;
            }
        }

        if (split.Length > 1)
        {
            if (int.TryParse(split[1], out var minor))
            {
                this.Minor = minor;
            }
        }

        if (split.Length > 2)
        {
            var patch = 0;
            if (int.TryParse(split[2], out patch))
            {
                this.Patch = patch;
            }
            else
            {
                var patchAndLabel = split[2].Split('-');
                if (int.TryParse(patchAndLabel[0], out patch))
                {
                    this.Patch = patch;
                }

                if (patchAndLabel.Length > 1)
                {
                    // patch might contain -RC for example for release candidate. or -unoffcial for unoffcial patches.
                    this.Label = patchAndLabel[1];
                }
            }
        }

    }
    public int CompareTo(SemVer other)
    {
        // If other is not a valid object reference, this instance is greater.
        if (other == null) return 1;

        if (this.Major > other.Major)
        {
            return 1;
        }

        if (this.Major < other.Major)
        {
            return -1;
        }

        if (this.Minor > other.Minor)
        {
            return 1;
        }

        if (this.Minor < other.Minor)
        {
            return -1;
        }

        if (this.Patch > other.Patch)
        {
            return 1;
        }

        if (this.Patch < other.Patch)
        {
            return -1;
        }

        return 0;

    }

    // Define the is greater than operator.
    public static bool operator >(SemVer operand1, SemVer operand2)
    {
        if (operand1 == null) return false;

        return operand1.CompareTo(operand2) > 0;
    }

    // Define the is less than operator.
    public static bool operator <(SemVer operand1, SemVer operand2)
    {
        if (operand1 == null) return false;

        return operand1.CompareTo(operand2) < 0;
    }

    // Define the is greater than or equal to operator.
    public static bool operator >=(SemVer operand1, SemVer operand2)
    {
        if (operand1 == null) return false;

        return operand1.CompareTo(operand2) >= 0;
    }

    // Define the is less than or equal to operator.
    public static bool operator <=(SemVer operand1, SemVer operand2)
    {
        if (operand1 == null) return false;
        return operand1.CompareTo(operand2) <= 0;
    }
}