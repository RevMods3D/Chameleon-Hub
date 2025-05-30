using System;
using System.Collections.Generic;

namespace Chameleon_Hub
{
    public static class FileName
    {
        // === 1. Resource Type IDs Mapped to Friendly Names ===
        private static readonly Dictionary<uint, string> ResourceNames = new()
        {
            // Graphics-related (0x00 to 0x0F)
            { 0x01, "Texture" },
            { 0x02, "Material" },
            { 0x03, "VertexDescriptor" },
            { 0x04, "VertexProgramState" },
            { 0x05, "Renderable" },
            { 0x06, "MaterialState" },
            { 0x07, "SamplerState" },
            { 0x08, "ShaderProgramBuffer" },

            // GeneSys
            { 0x10, "AttribSysSchema" },
            { 0x11, "AttribSysVault" },
            { 0x12, "GeneSysDefinition" },
            { 0x13, "GeneSysInstance" },
            { 0x14, "GenesysType" },
            { 0x15, "GenesysObject" },
            { 0x16, "BinaryFile" },

            // Indexing
            { 0x20, "EntryList" },
            { 0x21, "BundleIndex" },

            // Fonts and scripts
            { 0x30, "Font" },
            { 0x40, "LuaCode" },

            // Graphics (cont.)
            { 0x50, "InstanceList" },
            { 0x51, "Model" },
            { 0x52, "ColourCube" },
            { 0x53, "Shader" },

            // Collision
            { 0x60, "PolygonSoupList" },
            { 0x61, "PolygonSoupTree" },
            { 0x62, "IdList" },
            { 0x68, "NavigationMesh" },

            // Text
            { 0x70, "TextFile" },
            { 0x71, "TextFileList" },
            { 0x72, "ResourceHandleList" },
            { 0x74, "LuaData" },

            // Sound
            { 0x80, "Ginsu" },
            { 0x81, "Wave" },
            { 0x82, "WaveContainerTable" },
            { 0x83, "GameplayLinkData" },
            { 0x84, "WaveDictionary" },
            { 0x85, "MicroMonoStream" },
            { 0x86, "Reverb" },

            // Map
            { 0x90, "ZoneList" },
            { 0x91, "WorldPaintMap" },

            // Dictionaries
            { 0xA0, "IceAnimDictionary" },

            // Animation
            { 0xB0, "AnimationList" },
            { 0xB1, "PathAnimation" },
            { 0xB2, "Skeleton" },
            { 0xB3, "Animation" },

            // Shaders
            { 0xC0, "CgsVertexProgramState" },
            { 0xC1, "CgsProgramBuffer" },

            // Vehicles / Specs
            { 0x105, "VehicleList" },
            { 0x106, "VehicleGraphicsSpec" },
            { 0x107, "VehiclePhysicsSpec" },
            { 0x109, "WheelList" },
            { 0x10A, "WheelGraphicsSpec" },
            { 0x112, "EnvironmentKeyframe" },
            { 0x113, "EnvironmentTimeLine" },
            { 0x114, "EnvironmentDictionary" },
            { 0x116, "FlaptFile" },

            // Game data
            { 0x200, "AIData" },
            { 0x201, "Language" },
            { 0x202, "TriggerData" },
            { 0x203, "RoadData" },
            { 0x204, "DynamicInstanceList" },
            { 0x205, "WorldObject" },
            { 0x206, "ZoneHeader" },
            { 0x207, "VehicleSound" },
            { 0x208, "RoadMapData" },
            { 0x209, "CharacterSpec" },
            { 0x20A, "CharacterList" },
            { 0x20B, "SurfaceSounds" },
            { 0x20C, "ReverbRoadData" },
            { 0x20D, "CameraTake" },
            { 0x20E, "CameraTakeList" },
            { 0x20F, "GroundcoverCollection" },
            { 0x210, "ControlMesh" },
            { 0x211, "CutsceneData" },
            { 0x212, "CutsceneList" },
            { 0x213, "LightInstanceList" },
            { 0x214, "GroundcoverInstances" },
            { 0x215, "CompoundObject" },
            { 0x216, "CompoundInstanceList" },
            { 0x217, "PropObject" },
            { 0x218, "PropInstanceList" },
            { 0x219, "ZoneAmbienceList" },

            // FX and physics
            { 0x301, "BearEffect" },
            { 0x302, "BearGlobalParameters" },
            { 0x303, "ConvexHull" },

            // Traffic
            { 0x501, "HSMData" },
            { 0x700, "TrafficGraphicsStub" },
            { 0x701, "TrafficLaneData" }
        };

        // === 2. Known Specific File Hashes (like inside .dat files) ===
        private static readonly Dictionary<uint, string> KnownHashes = new()
        {
            { 0x30DF0100, "Aston Martin Vantage" },
            { 0x11223344, "Chevrolet Camaro Z28" }
            // Add more as you discover them
        };

        // === 3. Folder or BNDL file names ===
        private static readonly Dictionary<string, string> FolderNames = new()
        {
            { "SHAREDWAVES", "Shared Audio" },
            { "SURFACELIST", "Surface Materials" },
            { "VEHICLES", "Vehicle Data" },
            { "PVS", "Visibility Sets" },
            { "ZONES", "Map Zones" }
            // Add more as needed
        };

        // === === PUBLIC METHODS === ===

        /// <summary>
        /// Gets the friendly name from a resource type ID.
        /// </summary>
        public static string GetName(uint id)
        {
            return ResourceNames.TryGetValue(id, out var name) ? name : $"Unknown (0x{id:X})";
        }

        /// <summary>
        /// Gets the known name from a 4-byte hash inside a .dat file.
        /// </summary>
        public static string GetResourceHashName(uint hash)
        {
            return KnownHashes.TryGetValue(hash, out var name) ? name : $"0x{hash:X8}";
        }

        /// <summary>
        /// Parses the 4-byte resource code from byte array (assumes little-endian).
        /// </summary>
        public static uint ParseResourceId(byte[] bytes, int startIndex = 0)
        {
            if (bytes == null || bytes.Length < startIndex + 4)
                throw new ArgumentException("Insufficient bytes to parse resource ID.");
            return BitConverter.ToUInt32(bytes, startIndex);
        }

        /// <summary>
        /// Attempts to decode filenames like 00_72_01_00 using the second byte as resource type.
        /// </summary>
        public static string TryDecodeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains("_")) return fileName;

            var parts = fileName.Split('_');
            if (parts.Length != 4) return fileName;

            if (uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var typeId))
            {
                return GetName(typeId);
            }

            return fileName;
        }

        /// <summary>
        /// Gets a friendly name for a folder or BNDL-like file group.
        /// </summary>
        public static string GetFolderName(string folder)
        {
            return FolderNames.TryGetValue(folder.ToUpperInvariant(), out var name) ? name : folder;
        }
        private static readonly Dictionary<string, string> knownHeaders = new();
        public static string IdentifyContent(byte[] data)
        {
            if (data == null || data.Length < 4)
                return "unknown";

            string header = BitConverter.ToString(data, 0, 4).Replace("-", "");
            if (knownHeaders.TryGetValue(header, out string typeName))
            {
                return typeName;
            }

            return "unknown";
        }
    }
}
