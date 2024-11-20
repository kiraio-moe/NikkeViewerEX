using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

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
                Debug.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Add Spine Animation to GameObject.
        /// </summary>
        /// <param name="skelPath">Skeleton Animation asset path.</param>
        /// <param name="atlasPath">Skeleton Atlas asset path.</param>
        /// <param name="texturesPath">Skeleton textures asset path.</param>
        /// <param name="targetGameObject">The target GameObject to spawn Spine Skeleton Animation instance.</param>
        /// <param name="spineShader">The shader used for the Spine Skeleton Animation.</param>
        /// <param name="spineScale">The scale of the Spine Skeleton Animation.</param>
        /// <param name="spineScaleMultiplier">Spine Skeleton Animation scale multiplier.</param>
        /// <param name="loop">Loop Spine Skeleton Animation?</param>
        /// <param name="defaultAnimation">Default Spine Skeleton animation name to start.</param>
        /// <returns>The Skeleton Animation component it self.</returns>
        public static async UniTask<SkeletonAnimation> InstantiateSpine(
            string skelPath,
            string atlasPath,
            List<string> texturesPath,
            GameObject targetGameObject,
            Shader spineShader,
            float spineScale = 1f,
            float spineScaleMultiplier = 0.0115f,
            bool loop = false,
            string defaultAnimation = "idle"
        )
        {
            try
            {
                // Get atlas
                TextAsset atlasTextAsset = new(await WebRequestHelper.GetTextData(atlasPath));

                // Get image textures
                Texture2D[] imageTextures = new Texture2D[texturesPath.Count];
                byte[] imageData;

                for (int i = 0; i < texturesPath.Count; i++)
                {
                    Texture2D imageTexture = new(1, 1);
                    imageData = await WebRequestHelper.GetBinaryData(texturesPath[i]);

                    imageTexture.LoadImage(imageData);
                    imageTexture.name = Path.GetFileNameWithoutExtension(texturesPath[i]);
                    imageTextures[i] = imageTexture;
                }

                SpineAtlasAsset atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                    atlasTextAsset,
                    imageTextures,
                    spineShader,
                    true
                );

                AtlasAttachmentLoader attachmentLoader = new(atlasAsset.GetAtlas());
                SkeletonBinary skeletonBinary = new(attachmentLoader);
                skeletonBinary.Scale *= spineScaleMultiplier;
                skeletonBinary.Scale *= spineScale;

                SkeletonData skeletonData = skeletonBinary.ReadSkeletonData(skelPath);
                AnimationStateData animationStateData = new(skeletonData);
                SkeletonDataAsset skeletonDataAsset = CreateSkeletonDataAsset(
                    skeletonData,
                    animationStateData
                );

                SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(
                    targetGameObject,
                    skeletonDataAsset
                );

                skeletonAnimation.Initialize(false);
                // Set default skin that has any mesh data to prevent Degenerate Triangle error.
                foreach (Skin skin in skeletonData.Skins)
                {
                    if (CheckSkinMesh(skin))
                        skeletonAnimation.Skeleton.SetSkin(skin.Name);
                }
                skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                skeletonAnimation.AnimationState.SetAnimation(0, defaultAnimation, loop);
                skeletonAnimation.Update(0);
                skeletonAnimation.LateUpdate();

                return skeletonAnimation;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }
        }

        /// <summary>
        /// Check for any Mesh data in <paramref name="skin"/>.
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static bool CheckSkinMesh(Skin skin)
        {
            foreach (Skin.SkinEntry entry in skin.Attachments)
            {
                if (entry.Attachment is MeshAttachment meshAttachment)
                {
                    if (meshAttachment.Vertices.Length > 0 || meshAttachment.Triangles.Length > 0)
                        return true;
                }
            }
            return false;
        }

        public static Vector2 GetSkeletonBounds(Skeleton skeleton)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Slot slot in skeleton.Slots)
            {
                if (!slot.Bone.Active)
                    continue;

                Attachment attachment = slot.Attachment;
                if (attachment == null)
                    continue;

                float[] vertices = new float[8]; // Enough for a quad (4 vertices)

                if (attachment is RegionAttachment regionAttachment)
                {
                    // Get world vertices for RegionAttachment
                    regionAttachment.ComputeWorldVertices(slot.Bone, vertices, 0);
                }
                else if (attachment is MeshAttachment meshAttachment)
                {
                    // Get world vertices for MeshAttachment
                    int vertexCount = meshAttachment.WorldVerticesLength;
                    vertices = new float[vertexCount];
                    meshAttachment.ComputeWorldVertices(slot, 0, vertexCount, vertices, 0, 2);
                }
                else
                    continue; // Skip other attachment types

                // Update bounds
                for (int i = 0; i < vertices.Length; i += 2)
                {
                    float x = vertices[i];
                    float y = vertices[i + 1];
                    if (x < minX)
                        minX = x;
                    if (y < minY)
                        minY = y;
                    if (x > maxX)
                        maxX = x;
                    if (y > maxY)
                        maxY = y;
                }
            }

            // Calculate width and height
            float width = maxX - minX;
            float height = maxY - minY;

            return new Vector2(width, height);
        }

        /// <summary>
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Overlay).
        /// </summary>
        /// <param name="skeletonAnimation">The target Skeleton Animation.</param>
        /// <param name="bone">Bone name.</param>
        /// <returns>Bone screen position.</returns>
        public static Vector2 BoneScreenPosition(SkeletonAnimation skeletonAnimation, string bone)
        {
            return Camera.main.WorldToScreenPoint(
                skeletonAnimation
                    .skeleton.FindBone(bone)
                    .GetWorldPosition(skeletonAnimation.transform)
            );
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
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(
                skeletonAnimation
                    .skeleton.FindBone(bone)
                    .GetWorldPosition(skeletonAnimation.transform)
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                screenPoint,
                Camera.main,
                out Vector2 localPoint
            );
            return localPoint;
        }

        /// <summary>
        /// Loop through the provided bone names to get the available bone.
        /// Convert Bone world space coordinates into screen space coordinates (Screen Space - Overlay).
        /// </summary>
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
                Debug.Log($"Position: {bonePosition}");
                if (bonePosition != Vector2.negativeInfinity)
                {
                    Debug.Log($"Bone: {bone}, Position: {bonePosition}");
                    return bonePosition;
                }
            }

            Debug.LogWarning("No bones found!");
            return Vector2.negativeInfinity;
        }
    }
}
