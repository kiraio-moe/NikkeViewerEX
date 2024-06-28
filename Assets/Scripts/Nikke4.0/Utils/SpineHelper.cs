using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using Unity.Logging;

namespace NikkeViewerEX.Utils
{
    public class SpineHelper : SpineHelperBase
    {
        /// <summary>
        /// Equivalent to SkeletonDataAsset.CreateRuntimeInstance(), but accept SkeletonData and AnimationStateData as the arguments.
        /// </summary>
        /// <param>skeletonData</param>
        /// <param>stateData</param>
        public static SkeletonDataAsset CreateSkeletonDataAsset(
            SkeletonData skeletonData,
            AnimationStateData stateData
        )
        {
            try
            {
                // Create a new instance of SkeletonDataAsset
                SkeletonDataAsset skeletonDataAsset =
                    ScriptableObject.CreateInstance<SkeletonDataAsset>();

                // Get the type of SkeletonDataAsset
                Type skeletonDataAssetType = skeletonDataAsset.GetType();

                // Get the skeletonData and stateData fields
                FieldInfo skeletonDataField = skeletonDataAssetType.GetField(
                    "skeletonData",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                FieldInfo stateDataField = skeletonDataAssetType.GetField(
                    "stateData",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                // Set the values of skeletonData and stateData
                skeletonDataField.SetValue(skeletonDataAsset, skeletonData);
                stateDataField.SetValue(skeletonDataAsset, stateData);

                // Set a dummy value to skeletonJSON variable to make sure there's no
                // error returned if we call some method in SkeletonDataAsset
                skeletonDataAsset.skeletonJSON = new TextAsset("NIKKE");

                // Return the SkeletonDataAsset
                return skeletonDataAsset;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Add Spine Animation to GameObject.
        /// </summary>
        /// <param name="rootPath">The root path of the Spine Skeleton assets.</param>
        /// <param name="spineName">Base file name of the Spine Skeleton asset (without extension).</param>
        /// <param name="spineImages">List of full paths to the Spine Skeleton texture assets.</param>
        /// <param name="targetGameObject">The target GameObject to spawn Spine Skeleton Animation instance.</param>
        /// <param name="spineShader">The shader used for the Spine Skeleton Animation.</param>
        /// <param name="spineScale">The scale of the Spine Skeleton Animation.</param>
        /// <param name="spineScaleMultiplier">Spine Skeleton Animation scale multiplier.</param>
        /// <param name="loop">Loop Spine Skeleton Animation?</param>
        /// <param name="defaultAnimation">Default Spine Skeleton animation name to start.</param>
        /// <returns>The Skeleton Animation component it self.</returns>
        public static async Task<SkeletonAnimation> InstantiateSpine(
            string rootPath,
            string spineName,
            List<string> spineImages,
            GameObject targetGameObject,
            Shader spineShader,
            float spineScale = 1f,
            float spineScaleMultiplier = 0.0115f,
            bool loop = false,
            string defaultAnimation = "Start_Idle_01"
        )
        {
            try
            {
                string spinePath = Path.Combine(rootPath, spineName);
                string atlasPath = $"{spinePath}.atlas";
                string skelPath = $"{spinePath}.skel";

                // Get atlas
                TextAsset atlasTextAsset = new TextAsset(
                    await WebRequestHelper.GetTextData(atlasPath)
                );

                // Get image textures
                Texture2D[] imageTextures = new Texture2D[spineImages.Count];
                byte[] imageData;

                for (int i = 0; i < spineImages.Count; i++)
                {
                    string imageName = spineImages[i];
                    Texture2D imageTexture = new Texture2D(1, 1);
                    imageData = await WebRequestHelper.GetBinaryData(
                        Path.Combine(rootPath, $"{imageName}.png")
                    );

                    imageTexture.LoadImage(imageData);
                    imageTexture.name = imageName;
                    imageTextures[i] = imageTexture;
                }

                SpineAtlasAsset atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                    atlasTextAsset,
                    imageTextures,
                    spineShader,
                    true
                );

                AtlasAttachmentLoader attachmentLoader = new AtlasAttachmentLoader(
                    atlasAsset.GetAtlas()
                );
                SkeletonBinary skeletonBinary = new SkeletonBinary(attachmentLoader);
                skeletonBinary.Scale *= spineScaleMultiplier;
                skeletonBinary.Scale *= spineScale;

                SkeletonData skeletonData = skeletonBinary.ReadSkeletonData(skelPath);
                AnimationStateData animationStateData = new AnimationStateData(skeletonData);
                SkeletonDataAsset skeletonDataAsset = CreateSkeletonDataAsset(
                    skeletonData,
                    animationStateData
                );

                SkeletonAnimation spineAnimation = SkeletonAnimation.AddToGameObject(
                    targetGameObject,
                    skeletonDataAsset
                );
                spineAnimation.Initialize(false);
                spineAnimation.Skeleton.SetSlotsToSetupPose();
                spineAnimation.AnimationState.SetAnimation(0, defaultAnimation, loop);

                return spineAnimation;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Overlay).
        /// </summary>
        /// <param name="skeletonAnimation">The target Skeleton Animation.</param>
        /// <param name="bone">Bone name.</param>
        /// <returns>Bone screen position.</returns>
        public static Vector2 BoneScreenPosition(SkeletonAnimation skeletonAnimation, string bone)
        {
            // try
            // {
            return Camera.main.WorldToScreenPoint(
                skeletonAnimation
                    .skeleton.FindBone(bone)
                    .GetWorldPosition(skeletonAnimation.transform)
            );
            // }
            // catch
            // {
            //     return Vector2.negativeInfinity;
            // }
        }

        /// <summary>
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Camera).
        /// </summary>
        /// <param name="skeletonAnimation">The target Skeleton Animation.</param>
        /// <param name="bone">Bone name.</param>
        /// <returns>Bone screen position.</returns>
        public static Vector2 BoneScreenPosition(
            SkeletonAnimation skeletonAnimation,
            string bone,
            RectTransform rectTransform
        )
        {
            // try
            // {
            Vector2 localPoint,
                screenPoint = Camera.main.WorldToScreenPoint(
                    skeletonAnimation
                        .skeleton.FindBone(bone)
                        .GetWorldPosition(skeletonAnimation.transform)
                );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                screenPoint,
                Camera.main,
                out localPoint
            );
            return localPoint;
            // }
            // catch
            // {
            //     return Vector2.negativeInfinity;
            // }
        }

        /// <summary>
        /// Loop through the provided bone names to get the available bone.
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Overlay).
        /// /// </summary>
        /// <param name="skeletonAnimation">The target Skeleton Animation.</param>
        /// <param name="bone">Bone names list.</param>
        /// <returns>Bone screen position.</returns>
        public static Vector2 TryGetBoneScreenPosition(
            SkeletonAnimation skeletonAnimation,
            string[] bones
        )
        {
            foreach (string bone in bones)
            {
                Vector2 bonePosition = BoneScreenPosition(skeletonAnimation, bone);
                if (bonePosition != Vector2.negativeInfinity)
                    return bonePosition;
            }

            return Vector2.negativeInfinity;
        }

        /// <summary>
        /// Loop through the provided bone names to get the available bone.
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Camera).
        /// </summary>
        /// <param name="skeletonAnimation">The target Skeleton Animation.</param>
        /// <param name="bone">Bone names list.</param>
        /// <returns>Bone screen position.</returns>
        public static Vector2 TryGetBoneScreenPosition(
            SkeletonAnimation skeletonAnimation,
            string[] bones,
            RectTransform rectTransform
        )
        {
            foreach (string bone in bones)
            {
                Vector2 bonePosition = BoneScreenPosition(skeletonAnimation, bone, rectTransform);
                Log.Info($"Position: {bonePosition}");
                if (bonePosition != Vector2.negativeInfinity)
                {
                    Log.Info($"Bone: {bone}, Position: {bonePosition}");
                    return bonePosition;
                }
            }

            Log.Warning("No bones found!");
            return Vector2.negativeInfinity;
        }
    }
}
