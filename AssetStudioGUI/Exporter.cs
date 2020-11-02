using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using AssetStudio;
using Newtonsoft.Json;
using TGASharpLib;

namespace AssetStudioGUI
{
    internal static class Exporter
    {
        public static string ExportTexture2D(AssetItem item, string exportPath)
        {
            var m_Texture2D = (Texture2D)item.Asset;
            if (Properties.Settings.Default.convertTexture)
            {
                var bitmap = m_Texture2D.ConvertToBitmap(true);
                if (bitmap == null)
                    return null;
                ImageFormat format = null;
                var ext = Properties.Settings.Default.convertType;
                bool tga = false;
                switch (ext)
                {
                    case "BMP":
                        format = ImageFormat.Bmp;
                        break;
                    case "PNG":
                        format = ImageFormat.Png;
                        break;
                    case "JPEG":
                        format = ImageFormat.Jpeg;
                        break;
                    case "TGA":
                        tga = true;
                        break;
                }
                if (!TryExportFile(exportPath, item, "." + ext.ToLower(), out var exportFullPath))
                    return null;
                if (tga)
                {
                    var file = new TGA(bitmap);
                    file.Save(exportFullPath);
                }
                else
                {
                    bitmap.Save(exportFullPath, format);
                }
                bitmap.Dispose();
                return exportFullPath;
            }
            else
            {
                if (!TryExportFile(exportPath, item, ".tex", out var exportFullPath))
                    return null;
                File.WriteAllBytes(exportFullPath, m_Texture2D.image_data.GetData());
                return exportFullPath;
            }
        }

        public static string ExportAudioClip(AssetItem item, string exportPath)
        {
            var m_AudioClip = (AudioClip)item.Asset;
            var m_AudioData = m_AudioClip.m_AudioData.GetData();
            if (m_AudioData == null || m_AudioData.Length == 0)
                return null;
            var converter = new AudioClipConverter(m_AudioClip);
            if (Properties.Settings.Default.convertAudio && converter.IsSupport)
            {
                if (!TryExportFile(exportPath, item, ".wav", out var exportFullPath))
                    return null;
                var buffer = converter.ConvertToWav();
                if (buffer == null)
                    return null;
                File.WriteAllBytes(exportFullPath, buffer);
                return exportFullPath;
            }
            else
            {
                if (!TryExportFile(exportPath, item, converter.GetExtensionName(), out var exportFullPath))
                    return null;
                File.WriteAllBytes(exportFullPath, m_AudioData);
                return exportFullPath;
            }
        }

        public static string ExportShader(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".shader", out var exportFullPath))
                return null;
            var m_Shader = (Shader)item.Asset;
            var str = m_Shader.Convert();
            File.WriteAllText(exportFullPath, str);
            return exportFullPath;
        }

        public static string ExportTextAsset(AssetItem item, string exportPath)
        {
            var m_TextAsset = (TextAsset)(item.Asset);
            var extension = ".txt";
            if (Properties.Settings.Default.restoreExtensionName)
            {
                if (!string.IsNullOrEmpty(item.Container))
                {
                    extension = Path.GetExtension(item.Container);
                }
            }
            if (!TryExportFile(exportPath, item, extension, out var exportFullPath))
                return null;
            File.WriteAllBytes(exportFullPath, m_TextAsset.m_Script);
            return exportFullPath;
        }

        public static string ExportMonoBehaviour(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".json", out var exportFullPath))
                return null;
            var m_MonoBehaviour = (MonoBehaviour)item.Asset;
            var type = m_MonoBehaviour.ToType();
            if (type == null)
            {   
                try {
                    var nodes = Studio.MonoBehaviourToTypeTreeNodes(m_MonoBehaviour);
                    type = m_MonoBehaviour.ToType(nodes);
                }catch(System.Exception e)
                {
                    return null;
                }
            }
            var str = JsonConvert.SerializeObject(type, Formatting.Indented);
            File.WriteAllText(exportFullPath, str);
            return exportFullPath;
        }

        public static string ExportFont(AssetItem item, string exportPath)
        {
            var m_Font = (Font)item.Asset;
            if (m_Font.m_FontData != null)
            {
                var extension = ".ttf";
                if (m_Font.m_FontData[0] == 79 && m_Font.m_FontData[1] == 84 && m_Font.m_FontData[2] == 84 && m_Font.m_FontData[3] == 79)
                {
                    extension = ".otf";
                }
                if (!TryExportFile(exportPath, item, extension, out var exportFullPath))
                    return null;
                File.WriteAllBytes(exportFullPath, m_Font.m_FontData);
                return exportFullPath;
            }
            return null;
        }

        public static string ExportMesh(AssetItem item, string exportPath)
        {
            var m_Mesh = (Mesh)item.Asset;
            if (m_Mesh.m_VertexCount <= 0)
                return null;
            if (!TryExportFile(exportPath, item, ".obj", out var exportFullPath))
                return null;
            var sb = new StringBuilder();
            sb.AppendLine("g " + m_Mesh.m_Name);
            #region Vertices
            if (m_Mesh.m_Vertices == null || m_Mesh.m_Vertices.Length == 0)
            {
                return null;
            }
            int c = 3;
            if (m_Mesh.m_Vertices.Length == m_Mesh.m_VertexCount * 4)
            {
                c = 4;
            }
            for (int v = 0; v < m_Mesh.m_VertexCount; v++)
            {
                sb.AppendFormat("v {0} {1} {2}\r\n", -m_Mesh.m_Vertices[v * c], m_Mesh.m_Vertices[v * c + 1], m_Mesh.m_Vertices[v * c + 2]);
            }
            #endregion

            #region UV
            if (m_Mesh.m_UV0?.Length > 0)
            {
                if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 2)
                {
                    c = 2;
                }
                else if (m_Mesh.m_UV0.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vt {0} {1}\r\n", m_Mesh.m_UV0[v * c], m_Mesh.m_UV0[v * c + 1]);
                }
            }
            #endregion

            #region Normals
            if (m_Mesh.m_Normals?.Length > 0)
            {
                if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 3)
                {
                    c = 3;
                }
                else if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 4)
                {
                    c = 4;
                }
                for (int v = 0; v < m_Mesh.m_VertexCount; v++)
                {
                    sb.AppendFormat("vn {0} {1} {2}\r\n", -m_Mesh.m_Normals[v * c], m_Mesh.m_Normals[v * c + 1], m_Mesh.m_Normals[v * c + 2]);
                }
            }
            #endregion

            #region Face
            int sum = 0;
            for (var i = 0; i < m_Mesh.m_SubMeshes.Length; i++)
            {
                sb.AppendLine($"g {m_Mesh.m_Name}_{i}");
                int indexCount = (int)m_Mesh.m_SubMeshes[i].indexCount;
                var end = sum + indexCount / 3;
                for (int f = sum; f < end; f++)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\r\n", m_Mesh.m_Indices[f * 3 + 2] + 1, m_Mesh.m_Indices[f * 3 + 1] + 1, m_Mesh.m_Indices[f * 3] + 1);
                }
                sum = end;
            }
            #endregion

            sb.Replace("NaN", "0");
            File.WriteAllText(exportFullPath, sb.ToString());
            return exportFullPath;
        }

        public static string ExportVideoClip(AssetItem item, string exportPath)
        {
            var m_VideoClip = (VideoClip)item.Asset;
            var m_VideoData = m_VideoClip.m_VideoData.GetData();
            if (m_VideoData != null && m_VideoData.Length != 0)
            {
                if (!TryExportFile(exportPath, item, Path.GetExtension(m_VideoClip.m_OriginalPath), out var exportFullPath))
                    return null;
                File.WriteAllBytes(exportFullPath, m_VideoData);
                return exportFullPath;
            }
            return null;
        }

        public static string ExportMovieTexture(AssetItem item, string exportPath)
        {
            var m_MovieTexture = (MovieTexture)item.Asset;
            if (!TryExportFile(exportPath, item, ".ogv", out var exportFullPath))
                return null;
            File.WriteAllBytes(exportFullPath, m_MovieTexture.m_MovieData);
            return exportFullPath;
        }

        public static string ExportSprite(AssetItem item, string exportPath)
        {
            ImageFormat format = null;
            var type = Properties.Settings.Default.convertType;
            bool tga = false;
            switch (type)
            {
                case "BMP":
                    format = ImageFormat.Bmp;
                    break;
                case "PNG":
                    format = ImageFormat.Png;
                    break;
                case "JPEG":
                    format = ImageFormat.Jpeg;
                    break;
                case "TGA":
                    tga = true;
                    break;
            }
            if (!TryExportFile(exportPath, item, "." + type.ToLower(), out var exportFullPath))
                return null;
            var bitmap = ((Sprite)item.Asset).GetImage();
            if (bitmap != null)
            {
                if (tga)
                {
                    var file = new TGA(bitmap);
                    file.Save(exportFullPath);
                }
                else
                {
                    bitmap.Save(exportFullPath, format);
                }
                bitmap.Dispose();
                return exportFullPath;
            }
            return null;
        }

        public static string ExportRawFile(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".dat", out var exportFullPath))
                return null;
            File.WriteAllBytes(exportFullPath, item.Asset.GetRawData());
            return exportFullPath;
        }

        private static bool TryExportFile(string dir, AssetItem item, string extension, out string fullPath)
        {
            var fileName = FixFileName(item.Text);
            fullPath = Path.Combine(dir, fileName + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            fullPath = Path.Combine(dir, fileName + item.UniqueID + extension);
            if (!File.Exists(fullPath))
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            return false;
        }

        public static string ExportAnimator(AssetItem item, string exportPath, List<AssetItem> animationList = null)
        {
            var exportFullPath = Path.Combine(exportPath, item.Text, item.Text + ".fbx");
            if (File.Exists(exportFullPath))
            {
                exportFullPath = Path.Combine(exportPath, item.Text + item.UniqueID, item.Text + ".fbx");
            }
            var m_Animator = (Animator)item.Asset;
            var convert = animationList != null ? new ModelConverter(m_Animator, animationList.Select(x => (AnimationClip)x.Asset).ToArray()) : new ModelConverter(m_Animator);
            ExportFbx(convert, exportFullPath);
            return exportFullPath;
        }

        public static void ExportGameObject(GameObject gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var convert = animationList != null ? new ModelConverter(gameObject, animationList.Select(x => (AnimationClip)x.Asset).ToArray()) : new ModelConverter(gameObject);
            exportPath = exportPath + FixFileName(gameObject.m_Name) + ".fbx";
            ExportFbx(convert, exportPath);
        }

        public static void ExportGameObjectMerge(List<GameObject> gameObject, string exportPath, List<AssetItem> animationList = null)
        {
            var rootName = Path.GetFileNameWithoutExtension(exportPath);
            var convert = animationList != null ? new ModelConverter(rootName, gameObject, animationList.Select(x => (AnimationClip)x.Asset).ToArray()) : new ModelConverter(rootName, gameObject);
            ExportFbx(convert, exportPath);
        }

        private static void ExportFbx(IImported convert, string exportPath)
        {
            var eulerFilter = Properties.Settings.Default.eulerFilter;
            var filterPrecision = (float)Properties.Settings.Default.filterPrecision;
            var exportAllNodes = Properties.Settings.Default.exportAllNodes;
            var exportSkins = Properties.Settings.Default.exportSkins;
            var exportAnimations = Properties.Settings.Default.exportAnimations;
            var exportBlendShape = Properties.Settings.Default.exportBlendShape;
            var castToBone = Properties.Settings.Default.castToBone;
            var boneSize = (int)Properties.Settings.Default.boneSize;
            var scaleFactor = (float)Properties.Settings.Default.scaleFactor;
            var fbxVersion = Properties.Settings.Default.fbxVersion;
            var fbxFormat = Properties.Settings.Default.fbxFormat;
            ModelExporter.ExportFbx(exportPath, convert, eulerFilter, filterPrecision,
                exportAllNodes, exportSkins, exportAnimations, exportBlendShape, castToBone, boneSize, scaleFactor, fbxVersion, fbxFormat == 1);
        }

        public static string ExportDumpFile(AssetItem item, string exportPath)
        {
            if (!TryExportFile(exportPath, item, ".txt", out var exportFullPath))
                return null;
            var str = item.Asset.Dump();
            if (str == null && item.Asset is MonoBehaviour m_MonoBehaviour)
            {
                var nodes = Studio.MonoBehaviourToTypeTreeNodes(m_MonoBehaviour);
                str = m_MonoBehaviour.Dump(nodes);
            }
            if (str != null)
            {
                File.WriteAllText(exportFullPath, str);
                return exportFullPath;
            }
            return null;
        }

        public static string ExportConvertFile(AssetItem item, string exportPath)
        {
            switch (item.Type)
            {
                case ClassIDType.Texture2D:
                    return ExportTexture2D(item, exportPath);
                case ClassIDType.AudioClip:
                    return ExportAudioClip(item, exportPath);
                case ClassIDType.Shader:
                    return ExportShader(item, exportPath);
                case ClassIDType.TextAsset:
                    return ExportTextAsset(item, exportPath);
                case ClassIDType.MonoBehaviour:
                    return ExportMonoBehaviour(item, exportPath);
                case ClassIDType.Font:
                    return ExportFont(item, exportPath);
                case ClassIDType.Mesh:
                    return ExportMesh(item, exportPath);
                case ClassIDType.VideoClip:
                    return ExportVideoClip(item, exportPath);
                case ClassIDType.MovieTexture:
                    return ExportMovieTexture(item, exportPath);
                case ClassIDType.Sprite:
                    return ExportSprite(item, exportPath);
                case ClassIDType.Animator:
                     return ExportAnimator(item, exportPath);
                case ClassIDType.AnimationClip:
                    return null;
                default:
                    return ExportRawFile(item, exportPath);
            }
        }

        public static string FixFileName(string str)
        {
            if (str.Length >= 260) return Path.GetRandomFileName();
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c, '_'));
        }
    }
}
