//0 	OK
//1 	target exists/overwrite impossible, not requested
//2 	user canceled
//3 	no frame rate change
//4 	illegal frame rate
//5     illegal command on command line detected
//6     source file corrupt
//7     initial license was not accepted
//100 	unknown error

namespace FramerateConverter
{
    public enum ErrorCodes
    {
        Ok = 0,
        TargetExists = 1,
        Canceled = 2,
        NoFpsChange = 3,
        IllegalFps = 4,
        IllegalCommand = 5,
        SourceFileNoAcceptable = 6,
        LicenseNotAccepted = 7,
        TargetFileCorrupt = 8,
        Unknown = 100,
    }
}