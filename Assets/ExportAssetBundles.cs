using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ExportAssetBundles : MonoBehaviour
{

    [MenuItem("Assets/Build AssetBundle")]
    [System.Obsolete]
    static void ExportResource()
    {
        string path = "Assets/AssetBundle/cards.unity3d";
        Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        _ = BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path,
                                       BuildAssetBundleOptions.CollectDependencies
                                     | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneOSX);
    }
}