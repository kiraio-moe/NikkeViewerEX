using Cysharp.Threading.Tasks;
using NikkeViewerEX.Core;
using NikkeViewerEX.Utils;
using Spine.Unity;
using UnityEngine;

namespace NikkeViewerEX.Components
{
    [AddComponentMenu("Nikke Viewer EX/Components/Nikke Viewer 4.0")]
    public class NikkeViewer : NikkeViewerBase
    {
        SkeletonAnimation skeletonAnimation;

        void OnEnable()
        {
            MainControl.OnSettingsApplied += SpawnNikke;
        }

        void OnDestroy()
        {
            MainControl.OnSettingsApplied -= SpawnNikke;
        }

        async void SpawnNikke()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = await CreateNikke();
                AddMeshCollider();
            }
        }

        async UniTask<SkeletonAnimation> CreateNikke()
        {
            return await SpineHelper.InstantiateSpine(
                NikkeData.SkelPath,
                NikkeData.AtlasPath,
                NikkeData.TexturesPath,
                gameObject,
                Shader.Find("Universal Render Pipeline/Spine 4.0/Skeleton"),
                spineScale: 0.25f,
                loop: true
            );
        }
    }
}
