using LECodeBSTRMMassRenamer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LECodeBRSTMMassRenamer
{
    public class FileProcessor
    {
        public List<string> PossibleFinalLapSuffixes = new List<string>()
        {
            "_final","_f","(FinalLap)","(Final)","(Final Lap)","(Final Section)","(FinalSection)","(Last Lap)","(LastLap)","(Fast)"
        };
        string ConfigFileLocation;
        string BRSTMInputFolder;
        string BRSTMOutputFolder;
        string TracksBMGFile;
        string[] ConfigFileContents;
        string[] TracksFileContents;
        List<string> processedMessages = new List<string>();
        bool AllowFileOverwriting = false;
        bool ReversedMode = false;
        bool ClearOutputFolder = false;
        bool VerifyTrackFileExists = false;
        string TracksFolder;

        public FileProcessor(IConfiguration config)
        {
            ConfigFileLocation = config.GetSection("ConfigFile").Value;
            BRSTMInputFolder = config.GetSection("BRSTMInputFolder").Value;
            BRSTMOutputFolder = config.GetSection("BRSTMOutputFolder").Value;
            TracksBMGFile = config.GetSection("TracksBMGFile").Value;
            AllowFileOverwriting = bool.Parse(config.GetSection("AllowFileOverwriting").Value);
            ReversedMode = bool.Parse(config.GetSection("ReversedMode").Value);
            ClearOutputFolder = bool.Parse(config.GetSection("ClearOutputFolder").Value);
            VerifyTrackFileExists = bool.Parse(config.GetSection("VerifyTrackFileExists").Value);
            TracksFolder = config.GetSection("TracksFolder").Value;
        }

        public void RunProcessor()
        {
            if (string.IsNullOrEmpty(ConfigFileLocation) || string.IsNullOrEmpty(BRSTMInputFolder) || string.IsNullOrEmpty(BRSTMOutputFolder) || string.IsNullOrEmpty(TracksBMGFile) || (VerifyTrackFileExists && string.IsNullOrEmpty(TracksFolder)))
            {
                Console.WriteLine("ERROR:  Missing values for setting(s), please double check your appsettings.json file.");
            }
            BRSTMInputFolder = FormatFileDirectory(BRSTMInputFolder);
            BRSTMOutputFolder = FormatFileDirectory(BRSTMOutputFolder);
            if (!string.IsNullOrEmpty(TracksFolder))
            {
                TracksFolder = FormatFileDirectory(TracksFolder);
            }

            ConfigFileContents = System.IO.File.ReadAllLines(ConfigFileLocation);
            TracksFileContents = System.IO.File.ReadAllLines(TracksBMGFile);
            DirectoryInfo inputDir = new DirectoryInfo(BRSTMInputFolder);
            FileInfo[] files = inputDir.GetFiles("*.brstm");
            if(ClearOutputFolder)
            {
                Console.WriteLine("ClearOutputFolder enabled, clearing output folder.");
                DeleteExistingFilesInDirectory(BRSTMOutputFolder);
            }
            if(files.Length == 0)
            {
                Console.WriteLine("No brstm files found in input folder.");
            }
            else
            {
                foreach (string trackFileLine in TracksFileContents)
                {
                    ProcessTrack(trackFileLine, files);
                }
            }
            WriteFinalResults();
        }
        
        public void DeleteExistingFilesInDirectory(string directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            foreach(FileInfo file in directoryInfo.GetFiles())
            {
                Console.WriteLine($"Deleting {file.Name}.");
                file.Delete();
            }
        }

        public void ProcessTrack(string TracksLine, FileInfo[] files)
        {
            Console.WriteLine($"=======================");
            Console.WriteLine($"Processing {TracksLine}");
            string[] TracksLineSplit = TracksLine.Split("\t= ");
            if(TracksLineSplit.Length < 2)
            {
                Console.WriteLine("Line is not in proper format, skipping.");
                return;
            }
            //Verify this is a track based on memory address.
            string hexValue = TracksLineSplit[0].Trim();
            if(!Int32.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int memoryAddressInt)) {
                Console.WriteLine("This is not a hex value, skipping.");
                return;
            }
            int startingCourseMemAddress = Int32.Parse("7000", NumberStyles.HexNumber);
            if(memoryAddressInt < startingCourseMemAddress)
            {
                Console.WriteLine("This is not a track, skipping.");
                return;
            }

            hexValue = hexValue.Substring(1); //Must remove starting 7 to get hex file names
            
            string trackName = TracksLineSplit[1].Trim();

            if (VerifyTrackFileExists)
            {
                if (!File.Exists(TracksFolder + hexValue + ".szs"))
                {
                    Console.WriteLine(TracksFolder + hexValue + ".szs not found, skipping.");
                    return;
                }
            }

            //We've gotten far enough to start parsing the actual config file.
            string trackConfig = ConfigFileContents.FirstOrDefault(configLine => configLine.ToLower().Contains(trackName.ToLower()));
            if (trackConfig != null)
            {
                //Only tracks or hidden tracks are allowed
                if (!(trackConfig.Trim().StartsWith("T") || trackConfig.Trim().StartsWith("H")))
                {
                    Console.WriteLine("This is not a track, skipping.");
                    return;
                }
                TrackDefinition track = new TrackDefinition(trackConfig);
                string inputFileName;
                string outputFileName;
                if(ReversedMode)
                {
                    inputFileName = hexValue;
                    outputFileName = track.FileName;
                }
                else
                {
                    inputFileName = track.FileName;
                    outputFileName = hexValue;
                }
                files.Where(filesFound => filesFound.Name.StartsWith(inputFileName));
                FileInfo normalSpeedBrstm = files.FirstOrDefault(trackBrstm => trackBrstm.Name.ToLower() == inputFileName.ToLower() + ".brstm");
                CopyBRSTMFile(normalSpeedBrstm, false, outputFileName);
                FileInfo finalSpeedBrstm;
                int i = 0;
                while (i < PossibleFinalLapSuffixes.Count)
                {
                    finalSpeedBrstm = files.FirstOrDefault(trackBrstm =>
                        trackBrstm.Name.ToLower() == $"{inputFileName.ToLower()}{PossibleFinalLapSuffixes[i].ToLower()}.brstm"
                        || trackBrstm.Name.ToLower() == $"{inputFileName.ToLower()} {PossibleFinalLapSuffixes[i].ToLower()}.brstm");
                    if (finalSpeedBrstm != null)
                    {
                        CopyBRSTMFile(finalSpeedBrstm, true, outputFileName);
                        i = PossibleFinalLapSuffixes.Count;
                    }
                    i++;
                }           
            }
        }

        public void CopyBRSTMFile(FileInfo file, bool isFinal, string outputName)
        {
            //if found brstm, copy with hexValue as name.
            if (file != null)
            {
                string finalFileName = outputName;
                if (isFinal)
                {
                    finalFileName += "_f";
                }
                finalFileName += ".brstm";
                Console.WriteLine($"Creating file {BRSTMOutputFolder + finalFileName}");
                if(File.Exists(BRSTMOutputFolder + finalFileName) && !AllowFileOverwriting)
                {
                    Console.WriteLine($"File already exists {finalFileName}");
                    processedMessages.Add($"Attempted to create {finalFileName} from {file.Name}, but it already existed.");                  
                }
                else
                {
                    System.IO.File.Copy(file.FullName, BRSTMOutputFolder + finalFileName, AllowFileOverwriting);
                    processedMessages.Add($"Created {finalFileName} from {file.Name}.");
                }
                
            }
            else
            {
                Console.WriteLine("No BRSTM file found.");
            }
        }

        public void WriteFinalResults()
        {
            if (processedMessages.Count > 0)
            {
                Console.WriteLine("=================FINAL RESULTS=================");
                foreach (string message in processedMessages)
                {
                    Console.WriteLine(message);
                }
                Console.WriteLine($"Your files can be found at {BRSTMOutputFolder}");
            }
        }

        public string FormatFileDirectory(string directoryPath)
        {
            if (!directoryPath.EndsWith("\\")) {
                directoryPath += "\\";
            }
            return directoryPath;
        }
    }
}
