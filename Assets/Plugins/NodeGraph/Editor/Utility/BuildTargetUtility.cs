//using UnityEngine;
//using UnityEditor;

//using System;
//using System.IO;
//using System.Linq;
//using System.Collections.Generic;
//using System.Reflection;

//using Model=NodeGraph.DataModel;

//namespace UFrame.NodeGraph {
//	public class BuildTargetUtility {

//		public const BuildTargetGroup DefaultTarget = BuildTargetGroup.Unknown;

//		/**
//		 *  from build target to human friendly string for display purpose.
//		 */
	

//        public enum PlatformNameType {
//            Default,
//            TextureImporter,
//            AudioImporter,
//            VideoClipImporter
//        }

//        public static string TargetToAssetBundlePlatformName(BuildTargetGroup g, PlatformNameType pnt = PlatformNameType.Default) {
//            return TargetToAssetBundlePlatformName (GroupToTarget (g), pnt);
//        }

//		//returns the same value defined in AssetBundleManager
//        public static string TargetToAssetBundlePlatformName(BuildTarget t, PlatformNameType pnt = PlatformNameType.Default)
//		{
//			switch(t) {
//			case BuildTarget.Android:
//			return "Android";
//			case BuildTarget.iOS:
//                switch (pnt) {
//                case PlatformNameType.TextureImporter:
//                    return "iPhone";
//                }
//                return "iOS";
//			case BuildTarget.PS4:
//			return "PS4";
//			case BuildTarget.PSM:
//			return "PSM";
//			case BuildTarget.PSP2:
//                switch (pnt) {
//                case PlatformNameType.AudioImporter:
//                    return "PSP2";
//                case PlatformNameType.TextureImporter:
//                    return "PSP2";
//                case PlatformNameType.VideoClipImporter:
//                    return "PSP2";
//                }
//			return "PSVita";
//			case BuildTarget.SamsungTV:
//			return "Samsung TV";
//			case BuildTarget.StandaloneLinux:
//			case BuildTarget.StandaloneLinux64:
//			case BuildTarget.StandaloneLinuxUniversal:
//			return "Linux";
//			case BuildTarget.StandaloneOSXIntel:
//			case BuildTarget.StandaloneOSXIntel64:
//			case BuildTarget.StandaloneOSX:
//			return "OSX";
//            case BuildTarget.StandaloneWindows:
//            case BuildTarget.StandaloneWindows64:
//                switch (pnt) {
//                case PlatformNameType.AudioImporter:
//                    return "Standalone";
//                case PlatformNameType.TextureImporter:
//                    return "Standalone";
//                case PlatformNameType.VideoClipImporter:
//                    return "Standalone";
//                }
//			return "Windows";
//			case BuildTarget.Tizen:
//			return "Tizen";
//			case BuildTarget.tvOS:
//			return "tvOS";
//			case BuildTarget.WebGL:
//			return "WebGL";
//			case BuildTarget.WiiU:
//			return "WiiU";
//			case BuildTarget.WSAPlayer:
//                switch (pnt) {
//                case PlatformNameType.AudioImporter:
//                    return "WSA";
//                case PlatformNameType.VideoClipImporter:
//                    return "WSA";
//                }
//			return "WindowsStoreApps";
//			case BuildTarget.XboxOne:
//			return "XboxOne";
//#if !UNITY_5_5_OR_NEWER
//			case BuildTarget.Nintendo3DS:
//			return "N3DS";
//			case BuildTarget.PS3:
//			return "PS3";
//			case BuildTarget.XBOX360:
//			return "Xbox360";
//#endif
//#if UNITY_5_5_OR_NEWER
//			case BuildTarget.N3DS:
//			return "N3DS";
//#endif
//#if UNITY_5_6 || UNITY_5_6_OR_NEWER
//			case BuildTarget.Switch:
//			return "Switch";
//#endif

//			default:
//			return t.ToString() + "(deprecated)";
//			}
//		}

//		/**
//		 *  from build target group to human friendly string for display purpose.
//		 */
//		public static string GroupToHumaneString(UnityEditor.BuildTargetGroup g) {

//			switch(g) {
//			case BuildTargetGroup.Android:
//				return "Android";
//			case BuildTargetGroup.iOS:
//				return "iOS";
//			case BuildTargetGroup.PS4:
//				return "PlayStation 4";
//			case BuildTargetGroup.PSM:
//				return "PlayStation Mobile";
//			case BuildTargetGroup.PSP2:
//				return "PlayStation Vita";
//			case BuildTargetGroup.SamsungTV:
//				return "Samsung TV";
//			case BuildTargetGroup.Standalone:
//				return "PC/Mac/Linux Standalone";
//			case BuildTargetGroup.Tizen:
//				return "Tizen";
//			case BuildTargetGroup.tvOS:
//				return "tvOS";
//			case BuildTargetGroup.WebGL:
//				return "WebGL";
//			case BuildTargetGroup.WiiU:
//				return "Wii U";
//			case BuildTargetGroup.WSA:
//				return "Windows Store Apps";
//			case BuildTargetGroup.XboxOne:
//				return "Xbox One";
//			case BuildTargetGroup.Unknown:
//				return "Unknown";
//#if !UNITY_5_5_OR_NEWER
//			case BuildTargetGroup.Nintendo3DS:
//				return "Nintendo 3DS";
//			case BuildTargetGroup.PS3:
//				return "PlayStation 3";
//			case BuildTargetGroup.XBOX360:
//				return "Xbox 360";
//#endif
//#if UNITY_5_5_OR_NEWER
//			case BuildTargetGroup.N3DS:
//				return "Nintendo 3DS";
//#endif
//#if UNITY_5_6 || UNITY_5_6_OR_NEWER
//			case BuildTargetGroup.Facebook:
//				return "Facebook";
//			case BuildTargetGroup.Switch:
//				return "Nintendo Switch";
//#endif
//			default:
//				return g.ToString() + "(deprecated)";
//			}
//		}


//		public static BuildTargetGroup TargetToGroup(UnityEditor.BuildTarget t) {

//			if((int)t == int.MaxValue) {
//				return BuildTargetGroup.Unknown;
//			}

//			switch(t) {
//			case BuildTarget.Android:
//				return BuildTargetGroup.Android;
//			case BuildTarget.iOS:
//				return BuildTargetGroup.iOS;
//			case BuildTarget.PS4:
//				return BuildTargetGroup.PS4;
//			case BuildTarget.PSM:
//				return BuildTargetGroup.PSM;
//			case BuildTarget.PSP2:
//				return BuildTargetGroup.PSP2;
//			case BuildTarget.SamsungTV:
//				return BuildTargetGroup.SamsungTV;
//			case BuildTarget.StandaloneLinux:
//			case BuildTarget.StandaloneLinux64:
//			case BuildTarget.StandaloneLinuxUniversal:
//			case BuildTarget.StandaloneOSXIntel:
//			case BuildTarget.StandaloneOSXIntel64:
//			case BuildTarget.StandaloneOSX:
//			case BuildTarget.StandaloneWindows:
//			case BuildTarget.StandaloneWindows64:
//				return BuildTargetGroup.Standalone;
//			case BuildTarget.Tizen:
//				return BuildTargetGroup.Tizen;
//			case BuildTarget.tvOS:
//				return BuildTargetGroup.tvOS;
//			case BuildTarget.WebGL:
//				return BuildTargetGroup.WebGL;
//			case BuildTarget.WiiU:
//				return BuildTargetGroup.WiiU;
//			case BuildTarget.WSAPlayer:
//				return BuildTargetGroup.WSA;
//			case BuildTarget.XboxOne:
//				return BuildTargetGroup.XboxOne;
//#if !UNITY_5_5_OR_NEWER
//			case BuildTarget.Nintendo3DS:
//				return BuildTargetGroup.Nintendo3DS;
//			case BuildTarget.PS3:
//				return BuildTargetGroup.PS3;
//			case BuildTarget.XBOX360:
//				return BuildTargetGroup.XBOX360;
//#endif
//#if UNITY_5_5_OR_NEWER
//			case BuildTarget.N3DS:
//				return BuildTargetGroup.N3DS;
//#endif
//#if UNITY_5_6 || UNITY_5_6_OR_NEWER
//			case BuildTarget.Switch:
//				return BuildTargetGroup.Switch;
//#endif
//			default:
//				return BuildTargetGroup.Unknown;
//			}
//		}

//		public static BuildTarget GroupToTarget(UnityEditor.BuildTargetGroup g) {

//			switch(g) {
//			case BuildTargetGroup.Android:
//				return BuildTarget.Android;
//			case BuildTargetGroup.iOS:
//				return BuildTarget.iOS;
//			case BuildTargetGroup.PS4:
//				return BuildTarget.PS4;
//			case BuildTargetGroup.PSM:
//				return BuildTarget.PSM;
//			case BuildTargetGroup.PSP2:
//				return BuildTarget.PSP2;
//			case BuildTargetGroup.SamsungTV:
//				return BuildTarget.SamsungTV;
//			case BuildTargetGroup.Standalone:
//				return BuildTarget.StandaloneWindows;
//			case BuildTargetGroup.Tizen:
//				return BuildTarget.Tizen;
//			case BuildTargetGroup.tvOS:
//				return BuildTarget.tvOS;
//			case BuildTargetGroup.WebGL:
//				return BuildTarget.WebGL;
//			case BuildTargetGroup.WiiU:
//				return BuildTarget.WiiU;
//			case BuildTargetGroup.WSA:
//				return BuildTarget.WSAPlayer;
//			case BuildTargetGroup.XboxOne:
//				return BuildTarget.XboxOne;
//#if !UNITY_5_5_OR_NEWER
//			case BuildTargetGroup.Nintendo3DS:
//				return BuildTarget.Nintendo3DS;
//			case BuildTargetGroup.PS3:
//				return BuildTarget.PS3;
//			case BuildTargetGroup.XBOX360:
//				return BuildTarget.XBOX360;
//#endif
//#if UNITY_5_5_OR_NEWER
//			case BuildTargetGroup.N3DS:
//				return BuildTarget.N3DS;
//#endif
//#if UNITY_5_6 || UNITY_5_6_OR_NEWER
//			case BuildTargetGroup.Switch:
//				return BuildTarget.Switch;
//			case BuildTargetGroup.Facebook:
//				return BuildTarget.StandaloneWindows; // TODO: Facebook can be StandardWindows or WebGL
//#endif
//			default:
//				// temporarily assigned for default value (BuildTargetGroup.Unknown)
//				return (BuildTarget)int.MaxValue;
//			}
//		}

//		public static BuildTarget BuildTargetFromString (string val) {
//			return (BuildTarget)Enum.Parse(typeof(BuildTarget), val);
//		}

//		public static bool IsBuildTargetSupported(BuildTarget t) {

//			var objType = typeof(UnityEditor.BuildPipeline);
//			var method =  objType.GetMethod("IsBuildTargetSupported", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

//			#if UNITY_5_6 || UNITY_5_6_OR_NEWER
//			BuildTargetGroup g = BuildTargetUtility.TargetToGroup(t);
//			//internal static extern bool IsBuildTargetSupported (BuildTargetGroup buildTargetGroup, BuildTarget target);
//			var retval = method.Invoke(null, new object[]{
//				System.Enum.ToObject(typeof(BuildTargetGroup), g), 
//				System.Enum.ToObject(typeof(BuildTarget), t)});
//			#else 
//			//internal static extern bool IsBuildTargetSupported (BuildTarget target);
//			var retval = method.Invoke(null, new object[]{System.Enum.ToObject(typeof(BuildTarget), t)});
//			#endif
//			return Convert.ToBoolean(retval);
//		}
//	}
//}
