using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace zipto7z
{
    public class Program
    {
        public static string s7zlocation;
        static int Main(string[] args)
        {

            //locate 7z.dll!
            s7zlocation = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7-zip\\7z.dll";
            if (!System.IO.File.Exists(s7zlocation))
            {
                Console.WriteLine("7z dll not found in " + s7zlocation);
                s7zlocation = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\7-zip\\7z.dll";
                if (!System.IO.File.Exists(s7zlocation))
                    throw new Exception("7z dll not found in " + s7zlocation + ".  Program cannont continue \r\n Install 7-Zip!");
            }
            Console.WriteLine("7z DLL loaded from " + s7zlocation);
            SevenZip.SevenZipBase.SetLibraryPath(s7zlocation);

            if (args.Length >= 1 && File.Exists(args[0]))
                ConvertZip(args[0]);


            return 0;
        }


        public static void ConvertZip(string zipfile)
        {
            FileStream _7zfile = File.Create(Path.GetFileNameWithoutExtension(zipfile) + ".7z");
            Unzipper unzipfile = new Unzipper(zipfile);

            SevenZip.SevenZipCompressor _7zc = new SevenZip.SevenZipCompressor();
            Dictionary<string, Stream> newentry = new Dictionary<string, Stream>();
            foreach (string x in unzipfile.GetNextEntry())
            {

                
                _7zc.ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip;
                _7zc.CompressionLevel = SevenZip.CompressionLevel.High;
                _7zc.CompressionMode = SevenZip.CompressionMode.Create;
                _7zc.PreserveDirectoryRoot = true;
                _7zc.DirectoryStructure = true;
                _7zc.FastCompression = true;
                
                Console.WriteLine(x);
                System.IO.MemoryStream extractedzipfileentry = new MemoryStream();
                unzipfile.fz.ExtractFile(x, extractedzipfileentry);
                extractedzipfileentry.Position = 0;
                

                
                newentry.Add(x, extractedzipfileentry);
                //_7zc.CompressStream(extractedzipfileentry, _7zfile);
                
            }
            _7zc.CompressStreamDictionary(newentry, _7zfile);

            unzipfile.Dispose();
            _7zfile.Close();
        }


        private static System.Collections.Specialized.NameValueCollection GetArguments(string[] args)
        {
            System.Collections.Specialized.NameValueCollection nvc = new System.Collections.Specialized.NameValueCollection();
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (i < args.Length - 1)
                    {
                        if (indexOfSwitch(args[i]) != 0 && indexOfSwitch(args[i + 1]) == 0)
                        {
                            //follows "-switch switchargument "
                            nvc.Add(args[i].Substring(indexOfSwitch(args[i])), args[i + 1]);
                            i++;
                            continue;
                        }
                        if (indexOfSwitch(args[i]) != 0 && indexOfSwitch(args[i + 1]) != 0)
                        {
                            //follows "-switch -switch2"
                            nvc.Add(args[i].Substring(indexOfSwitch(args[i])), null);
                            i++;
                            continue;
                        }
                    }
                    //Not a switch or an argument to a switch?  
                    //nvc.Add(null, args[i]);
                    nvc.Add(i.ToString(), args[i]);
                }
            }

            return nvc;
        }
        private static int indexOfSwitch(string arg)
        {
            if (String.IsNullOrEmpty(arg))
                return 0;
            int singledash = arg.IndexOf("-");
            int doubledash = arg.IndexOf("--");
            int slash = arg.IndexOf("/");
            if (doubledash == 0)
                return 2;
            if (singledash == 0 || slash == 0)
                return 1;
            return 0;
        }

    }




    /// <summary>
    /// Class that will lazily enumerate each zip entry in a zipfile
    /// </summary>
    public class Unzipper : IDisposable
    {

        private bool disposedValue = false; // To detect redundant calls
        private string _zipfile;

        public SevenZip.SevenZipExtractor fz;

        public Unzipper(string zipfile)
        {
            _zipfile = zipfile;
            fz = new SevenZip.SevenZipExtractor(zipfile);
        }

        public IEnumerable<string> GetNextEntry()
        {
            int zipi = 0;
            foreach (string x in fz.ArchiveFileNames)
            {
                zipi++;
                if (String.IsNullOrEmpty(x))
                    continue;
                yield return x;
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    fz.Dispose();

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Unzipper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
