using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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

            NameValueCollection nvcargs = GetArguments(args);
            bool inmemory = false;
            bool deleteafterconvert = false;
            if (nvcargs.AllKeys.Contains("memory"))
                inmemory = true;
            if (nvcargs.AllKeys.Contains("delete"))
                deleteafterconvert = true;
            //Console.Write("Memory argument: {0}   Delete argument: {1}", inmemory, deleteafterconvert);
            if (args.Length >= 1 && File.Exists(args[0]))
            {
                if (!inmemory)
                    convertZipInFileSystem(args[0]);
                else
                    convertZipinMemory(args[0]);
                if (deleteafterconvert)
                    File.Delete(args[0]);
            }
            return 0;
        }


        public static void convertZipInFileSystem(string zipfile)
        {
            FileStream _7zfile = File.Create(Path.GetFileNameWithoutExtension(zipfile) + ".7z");
            SevenZip.SevenZipExtractor ze = new SevenZip.SevenZipExtractor(zipfile);
            string tempdirectory = Path.Combine(Environment.CurrentDirectory, zipfile + "7z");
            Directory.CreateDirectory(tempdirectory);
            ze.ExtractArchive(tempdirectory);
            ze.Dispose();

            SevenZip.SevenZipCompressor _7zc = new SevenZip.SevenZipCompressor();
            Dictionary<string, string> newentry = new Dictionary<string, string>();
            _7zc.ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip;
            _7zc.CompressionLevel = SevenZip.CompressionLevel.High;
            _7zc.CompressionMode = SevenZip.CompressionMode.Create;
            _7zc.DirectoryStructure = true;
            _7zc.FastCompression = true;
            Console.WriteLine("Beginning compression.  This will take a while...");
            _7zc.CompressDirectory(tempdirectory, _7zfile);


            _7zfile.Close();
            Directory.Delete(tempdirectory, true);
            Console.WriteLine("Done.");
        }


        public static void convertZipinMemory(string zipfile)
        {
            FileStream _7zfile = File.Create(Path.GetFileNameWithoutExtension(zipfile) + ".7z");
            Unzipper unzipfile = new Unzipper(zipfile);

            SevenZip.SevenZipCompressor _7zc = new SevenZip.SevenZipCompressor();
            Dictionary<string, Stream> newentry = new Dictionary<string, Stream>();
            _7zc.ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip;
            _7zc.CompressionLevel = SevenZip.CompressionLevel.High;
            _7zc.CompressionMode = SevenZip.CompressionMode.Create;
            _7zc.PreserveDirectoryRoot = true;
            _7zc.DirectoryStructure = true;
            _7zc.FastCompression = true;

            foreach (string x in unzipfile.GetNextEntry())
            {
                System.IO.MemoryStream extractedzipfileentry = new MemoryStream();
                unzipfile.fz.ExtractFile(x, extractedzipfileentry);
                extractedzipfileentry.Position = 0;

                newentry.Add(x, extractedzipfileentry);

            }
            Console.WriteLine("Loaded {0} entries, beginning compression.  This will take a while...", newentry.Count);
            _7zc.CompressStreamDictionary(newentry, _7zfile);

            unzipfile.Dispose();
            _7zfile.Close();
            Console.WriteLine("Done.");
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

                            continue;
                        }
                    }
                    else
                    {
                        if (indexOfSwitch(args[i]) != 0)
                            nvc.Add(args[i].Substring(indexOfSwitch(args[i])), null);
                        else  //Not a switch or an argument to a switch?  
                            nvc.Add(i.ToString(), args[i]);
                    }
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