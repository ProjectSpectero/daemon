namespace Spectero.daemon.Libraries.Marhsal
{
    public class MarshalUtil
    {
        /// <summary>
        /// Marshal Decoder
        /// This is a utility function that serves to assist with error detection.
        /// </summary>
        /// <param name="marshall"></param>
        /// <returns></returns>
        public static string DecodeIntToString(int marshall)
        {
            switch (marshall)
            {
                case 0:
                    return "ERROR_SUCCESS";
                case 1:
                    return "ERROR_INVALID_FUNCTION";
                case 2:
                    return "ERROR_FILE_NOT_FOUND";
                case 3:
                    return "ERROR_PATH_NOT_FOUND";
                default:
                    return marshall.ToString();
            }
        }
    }
}