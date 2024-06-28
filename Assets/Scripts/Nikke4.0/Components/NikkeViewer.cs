using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Logging;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using NikkeViewerEX.Utils;

namespace NikkeViewerEX.Components
{
    [AddComponentMenu("Nikke Viewer EX/Components/Nikke Viewer 4.0")]
    public class NikkeViewer : NikkeViewerBase
    {
        SkeletonAnimation nikkeAnimation;

        async UniTaskVoid Start()
        {
            Log.Info($"Nikke {Path.Combine(StorageHelper.GetApplicationPath(), "c997_00")}");

            nikkeAnimation = await SpineHelper.InstantiateSpine(
                StorageHelper.GetApplicationPath(),
                "c010_00",
                new List<string> { "c010_00" },
                gameObject,
                Shader.Find("Universal Render Pipeline/Spine 4.0/Skeleton"),
                spineScale: 0.25f,
                loop: true,
                defaultAnimation: "idle"
            );
        }
    }
}
