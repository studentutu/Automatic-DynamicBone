﻿using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Jobs;
using Unity.Collections;

namespace ADBRuntime
{
    using Mono;
    public class ADBRuntimeColliderControll
    {
        public List<ADBRuntimeCollider> runtimeColliderList;

        public bool initialized = false;
        private ColliderRead[] collidersReadTable;
        private ColliderReadWrite[] collidersReadWriteTable;
        private Transform[] colliderTransform;

        #region Point and parameter


        public const float upperArmWidthAspect = 1f;
        public const float lowerArmWidthAspect = 0.9f;
        public const float endArmWidthAspect = 0.81f;

        public const float upperLegWidthAspect = 1f;
        public const float lowerLegWidthAspect = 0.7f;
        public const float endLegWidthAspect = 0.7f;

        public Vector3 rootPoint;
        public Vector3 headStartPoint;
        public Vector3 headCenterPoint;
        public float headColliderRadiu;

        public Vector3 spineStopPoint;
        public Vector3 spineStartPoint;
        public float spineColliderRadiu;

        public Vector3 hipsStopPoint;
        private float hipsColliderRadiuUp;
        private float hipsColliderRadiuDown;
        public Vector3 hipsStartPoint;

        public Vector3 upperArmToHeadCentroid;
        public Vector3 upperLegCentroid;

        public Vector3 leftHandCenterPoint;
        public Vector3 rightHandCenterPoint;
        public Vector3 leftFootCenterPoint;
        public Vector3 rightFootCenterPoint;

        public float torsoWidth;
        public float hipsWidth;
        public float headToRootHigh;
        #endregion

        public ADBRuntimeColliderControll(GameObject character, List<ADBRuntimePoint> allPointTrans, bool isGenerateBodyRuntimeCollider,bool isGenerateScript,bool isGenerateFinger, out List<ADBEditorCollider> editorColliderList )
        {
            runtimeColliderList = new List<ADBRuntimeCollider>();
            editorColliderList = new List<ADBEditorCollider>();

            initialized = false;
            bool iniA = false, iniB=false, iniC=false; 
            if (isGenerateBodyRuntimeCollider)
            {
                iniA = GenerateBodyCollidersData(ref runtimeColliderList, character, allPointTrans, isGenerateFinger);
            }
            if (isGenerateScript)
            {
                for (int i = 0; i < runtimeColliderList.Count; i++)
                {
                    if (runtimeColliderList[i].appendTransform == null) continue;
               
                    ADBEditorCollider.RuntimeCollider2Editor(runtimeColliderList[i]);
                }
            }
            iniB = GenerateOtherCollidersData(ref runtimeColliderList,ref editorColliderList, character);
            for (int i = 0; i < runtimeColliderList.Count; i++)
            {//OYM：这个设置可以帮助你的collider跟随你的角色一起发生尺寸变化
                //OYM：某些特殊的情况,你可以关闭它
                runtimeColliderList[i].colliderRead.isConnectWithBody = true;
            }
            iniC = GenerateGlobalCollidersData(ref runtimeColliderList);

            initialized = iniA || iniB || iniC;

            if (initialized && Application.isPlaying)
            {
                colliderTransform = new Transform[runtimeColliderList.Count];
                collidersReadTable = new ColliderRead[runtimeColliderList.Count];
                collidersReadWriteTable = new ColliderReadWrite[runtimeColliderList.Count];

                for (int i = 0; i < runtimeColliderList.Count; i++)
                {
                    collidersReadTable[i] = runtimeColliderList[i].GetColliderRead();
                    colliderTransform[i] = runtimeColliderList[i].appendTransform;
                }
            }
            if (!initialized)
            {
                Debug.Log("SomeThing in ADBRuntimeColliderControll is wrong....");
            }
        }

        private bool GenerateGlobalCollidersData(ref List<ADBRuntimeCollider> runtimeColliderList)
        {
            if (Application.isPlaying&&ADBEditorCollider.globalColliderList != null)
            {
                for (int i = 0; i < ADBEditorCollider.globalColliderList.Count; i++)
                {
                    if (!runtimeColliderList.Contains(ADBEditorCollider.globalColliderList[i]))
                    {
                        runtimeColliderList.Add(ADBEditorCollider.globalColliderList[i]);
                    }
                }
                return true;
            }
            return false;
        }

        internal void GetData(ref DataPackage dataPackage)
        {
            if (!initialized) return;
            dataPackage.SetColliderPackage(collidersReadTable, collidersReadWriteTable, colliderTransform);
        }

        private bool GenerateBodyCollidersData(ref List<ADBRuntimeCollider> runtimeColliderList, GameObject character, List<ADBRuntimePoint> allPointTrans,bool isGenerateFinger)
        {
            if (!character) return false;

            var animator = character.GetComponent<Animator>();
            if (animator != null && animator.avatar.isHuman)
            {
                GenerateCollidersData(ref runtimeColliderList, allPointTrans, animator, isGenerateFinger);
                return true;

            }
            else
            {
                var animators = character.GetComponentsInChildren<Animator>();
                if (animators != null && animators.Length != 0)
                {
                    bool isFind = false;
                    for (int i = 0; i < animators.Length; i++)
                    {
                        isFind = isFind || GenerateBodyCollidersData(ref runtimeColliderList, animators[i].gameObject, allPointTrans, isGenerateFinger);
                    }
                }
                else
                {
                    Debug.Log(character.name + "'s Avatar is lost or isn't Human!");
                }
            }
            return false;
        }

        private bool GenerateOtherCollidersData(ref List<ADBRuntimeCollider> runtimeColliderList, ref List<ADBEditorCollider> editorColliderList, GameObject character)
        {
            ADBEditorCollider[] colliderList = character.GetComponentsInChildren<ADBEditorCollider>(true);
            foreach (var collider in colliderList)
            {
                var co = collider.editor;

                if (co != null)
                {
                    runtimeColliderList.Add(co);
                }
            }
            if (editorColliderList != null)
            {
                editorColliderList.AddRange(colliderList);
            }
            return true;
        }

        public void OnDrawGizmos()
        {
            if (!initialized) return;

            for (int i = 0; i < runtimeColliderList.Count; i++)
            {
                if (runtimeColliderList[i].appendTransform)
                {
                    runtimeColliderList[i].OnDrawGizmos();
                }
            }
        }


        private void GenerateCollidersData(ref List<ADBRuntimeCollider> runtimeColliders, List<ADBRuntimePoint> allPointTrans, Animator animator,bool isGenerateFinger)
        {//OYM：这坨屎山我连写注释的兴趣都没有,你知道这玩意能大概把你角色圈进去就行
            //OYM：你问我怎么算的?当然是经验(试出来)啦 XD
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
            var spine = animator.GetBoneTransform(HumanBodyBones.Spine);

            var leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);

            var rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);

            var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var leftFinger = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);

            var rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var rightFinger = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);

            rootPoint = animator.transform.position;
            headStartPoint = head.position;

            upperArmToHeadCentroid = 0.5f * (leftUpperArm.position + rightUpperArm.position);
            torsoWidth = Vector3.Distance(leftUpperArm.position, rightUpperArm.position);

            upperLegCentroid = 0.5f * (leftUpperLeg.position + rightUpperLeg.position);
            hipsWidth = Vector3.Distance(leftUpperLeg.position, rightUpperLeg.position);
            var colliderList = new List<ADBRuntimeCollider>();

            //OYM：Head
            headCenterPoint = headStartPoint + new Vector3(0, 0.5f * torsoWidth, 0);
            headColliderRadiu = CheckNearstPointToSegment(0.5f * torsoWidth, headCenterPoint, Vector3.zero, ColliderChoice.Head, allPointTrans);
            colliderList.Add(new SphereCollider(headColliderRadiu, head.InverseTransformPoint(head.position+ new Vector3(0, 0.5f * torsoWidth, 0)),ColliderChoice.Head, head));

            // Spine
            spineStartPoint = headCenterPoint + new Vector3(0,  - torsoWidth, 0);
            spineStopPoint = upperLegCentroid;
            spineColliderRadiu = CheckNearstPointToSegment(torsoWidth, spineStartPoint, spineStopPoint - spineStartPoint, ColliderChoice.UpperBody, allPointTrans);

            colliderList.Add(new CapsuleCollider(spineColliderRadiu, spineStartPoint, spineStopPoint, ColliderChoice.UpperBody, spine));

            //Hip

            Vector3 hipColliderCenter =upperLegCentroid;

            hipsColliderRadiuUp = CheckNearstPointToSegment((spineColliderRadiu *2), hipColliderCenter, Vector3.zero, ColliderChoice.UpperBody, allPointTrans);
            colliderList.Add(new SphereCollider(hipsColliderRadiuUp,spine.InverseTransformPoint(hipColliderCenter) , ColliderChoice.UpperBody, spine));
            Vector3 hipColliderCenterDownA = upperLegCentroid- new Vector3(hipsWidth*0.5f, 0, 0);
            Vector3 hipColliderCenterDownB = hipColliderCenterDownA + new Vector3(hipsWidth, 0,0);
            hipsColliderRadiuDown = CheckNearstPointToSegment(hipsWidth, hipColliderCenterDownA, hipColliderCenterDownB- hipColliderCenterDownA, ColliderChoice.LowerBody, allPointTrans);

            colliderList.Add(new CapsuleCollider(hipsColliderRadiuDown, hipColliderCenterDownA, hipColliderCenterDownB, ColliderChoice.LowerBody, pelvis));


            // LeftArms

            float leftArmWidth = Vector3.Distance(leftUpperArm.position, leftLowerArm.position) * 0.3f;
            float leftUpperArmWidth = CheckNearstPointToSegment(leftArmWidth * upperArmWidthAspect, leftUpperArm.position, leftLowerArm.position - leftUpperArm.position, ColliderChoice.UpperArm, allPointTrans);
            float leftLowerArmWidth = CheckNearstPointToSegment(leftArmWidth * lowerArmWidthAspect, leftLowerArm.position, leftHand.position - leftLowerArm.position, ColliderChoice.LowerArm, allPointTrans);

            colliderList.Add(new CapsuleCollider(leftUpperArmWidth, leftUpperArm.position, leftLowerArm.position, ColliderChoice.UpperArm, leftUpperArm));
            colliderList.Add(new CapsuleCollider(leftLowerArmWidth, leftLowerArm.position, leftHand.position, ColliderChoice.LowerArm, leftLowerArm));
            var leftHandCenterPoint = (leftFinger.position + leftHand.position) * 0.5f;




            // LeftLegs
            float leftLegWidth = Vector3.Distance(leftUpperLeg.position, leftLowerLeg.position) * 0.3f;
            float leftUpperLegWidth = CheckNearstPointToSegment(leftLegWidth * upperLegWidthAspect, leftUpperLeg.position, leftLowerLeg.position - leftUpperLeg.position, ColliderChoice.UpperLeg, allPointTrans);
            float leftLowerLegWidth = CheckNearstPointToSegment(leftLegWidth * lowerLegWidthAspect, leftLowerLeg.position, leftHand.position - leftLowerLeg.position, ColliderChoice.LowerLeg, allPointTrans);
            float leftEndLegWidth = leftLegWidth * endLegWidthAspect;

            colliderList.Add(new CapsuleCollider(leftUpperLegWidth, leftUpperLeg.position - new Vector3(0, leftUpperLegWidth, 0), leftLowerLeg.position,ColliderChoice.UpperLeg, leftUpperLeg));

            colliderList.Add(new CapsuleCollider(leftLowerLegWidth, leftLowerLeg.position, leftFoot.position, ColliderChoice.LowerLeg, leftLowerLeg));
            // LeftFoot

            if (leftToes != null)
            {
                colliderList.Add(new CapsuleCollider(leftEndLegWidth, leftFoot.position, leftToes.position, ColliderChoice.Foot,leftFoot));
            }
            else
            {
                Vector3 leftfootStartPoint = leftFoot.position;
                Vector3 leftfootStopPoint = new Vector3(leftfootStartPoint.x, animator.rootPosition.y + leftEndLegWidth, leftfootStartPoint.z) + animator.rootRotation * Vector3.forward * (leftLowerArm.position - leftHand.position).magnitude * endLegWidthAspect;
                colliderList.Add(new CapsuleCollider(leftEndLegWidth, leftfootStartPoint, leftfootStopPoint, ColliderChoice.Foot, leftFoot));
            }

            // rightArms

            float rightArmWidth = Vector3.Distance(rightUpperArm.position, rightLowerArm.position) * 0.3f;
            float rightUpperArmWidth = CheckNearstPointToSegment(rightArmWidth * upperArmWidthAspect, rightUpperArm.position, rightLowerArm.position - rightUpperArm.position, ColliderChoice.UpperArm, allPointTrans);
            float rightLowerArmWidth = CheckNearstPointToSegment(rightArmWidth * lowerArmWidthAspect, rightLowerArm.position, rightHand.position - rightLowerArm.position, ColliderChoice.LowerArm, allPointTrans);

            colliderList.Add(new CapsuleCollider(rightUpperArmWidth, rightUpperArm.position, rightLowerArm.position, ColliderChoice.UpperArm, rightUpperArm));
            colliderList.Add(new CapsuleCollider(rightLowerArmWidth, rightLowerArm.position, rightHand.position, ColliderChoice.LowerArm, rightLowerArm));
            var rightHandCenterPoint = (rightFinger.position + rightHand.position) * 0.5f;



            // rightLegs
            float rightLegWidth = Vector3.Distance(rightUpperLeg.position, rightLowerLeg.position) * 0.3f;
            float rightUpperLegWidth = CheckNearstPointToSegment(rightLegWidth * upperLegWidthAspect, rightUpperLeg.position, rightLowerLeg.position - rightUpperLeg.position, ColliderChoice.UpperLeg, allPointTrans);
            float rightLowerLegWidth = CheckNearstPointToSegment(rightLegWidth * lowerLegWidthAspect, rightLowerLeg.position, rightHand.position - rightLowerLeg.position, ColliderChoice.LowerLeg, allPointTrans);
            float rightEndLegWidth = rightLegWidth * endLegWidthAspect;

            colliderList.Add(new CapsuleCollider(rightUpperLegWidth, rightUpperLeg.position - new Vector3(0, rightUpperLegWidth, 0), rightLowerLeg.position, ColliderChoice.UpperLeg, rightUpperLeg));

            colliderList.Add(new CapsuleCollider(rightLowerLegWidth, rightLowerLeg.position, rightFoot.position, ColliderChoice.LowerLeg, rightLowerLeg));
            // rightFoot

            if (rightToes != null)
            {
                colliderList.Add(new CapsuleCollider(rightEndLegWidth, rightFoot.position, rightToes.position, ColliderChoice.Foot, rightFoot));
            }
            else
            {
                Vector3 rightfootStartPoint = rightFoot.position;
                Vector3 rightfootStopPoint = new Vector3(rightfootStartPoint.x, animator.rootPosition.y + rightEndLegWidth, rightfootStartPoint.z) + animator.rootRotation * Vector3.forward * (rightLowerArm.position - rightHand.position).magnitude * endLegWidthAspect;
                colliderList.Add(new CapsuleCollider(rightEndLegWidth, rightfootStartPoint, rightfootStopPoint, ColliderChoice.Foot, rightFoot));
            }

            if (!isGenerateFinger)
            {
                colliderList.Add(new SphereCollider(Vector3.Distance(leftHand.position, leftHandCenterPoint), leftHand.InverseTransformPoint(leftHandCenterPoint), ColliderChoice.Hand, leftHand));//OYM:lefthand
                colliderList.Add(new SphereCollider(Vector3.Distance(rightHand.position, rightHandCenterPoint), rightHand.InverseTransformPoint(rightHandCenterPoint), ColliderChoice.Hand, rightHand));//OYM:righthand
            }
            else
            {
                //OYM：Left
                var leftThumb1 = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                var leftThumb2 = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
                var leftThumb3 = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
                var leftIndex1 = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
                var leftIndex2 = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
                var leftIndex3 = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
                var leftMiddle1 = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
                var leftMiddle2 = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
                var leftMiddle3 = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal); 
                var leftRing1 = animator.GetBoneTransform(HumanBodyBones.LeftRingProximal);
                var leftRing2 = animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate);
                var leftRing3 = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
                var leftLittle1 = animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal);
                var leftLittle2 = animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate);
                var leftLittle3 = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);

                var leftHandLength = (leftMiddle2.position - leftHand.position).magnitude;
                //  colliderList.Add(new OBBBoxCollider(leftHandCenterPoint, new Vector3(leftHandLength * 0.25f, leftHandLength, leftHandLength), leftMiddle2.position - leftMiddle1.position , ColliderChoice.Hand, leftHand));

                colliderList.Add(new CapsuleCollider((leftThumb1.position - leftThumb2.position).magnitude / 2, leftThumb1.position, leftThumb2.position, ColliderChoice.Hand, leftThumb1));
                colliderList.Add(new CapsuleCollider((leftThumb2.position - leftThumb3.position).magnitude / 2, leftThumb2.position, leftThumb3.position, ColliderChoice.Hand, leftThumb2));
                colliderList.Add(new CapsuleCollider((leftThumb2.position - leftThumb3.position).magnitude / 2 * 0.8f, leftThumb3.position, leftThumb3.position + (leftThumb3.position - leftThumb2.position) * 0.8f, ColliderChoice.Hand, leftThumb3));
                colliderList.Add(new CapsuleCollider((leftIndex1.position - leftIndex2.position).magnitude / 2, leftIndex1.position, leftIndex2.position, ColliderChoice.Hand, leftIndex1));
                colliderList.Add(new CapsuleCollider((leftIndex2.position - leftIndex3.position).magnitude / 2, leftIndex2.position, leftIndex3.position, ColliderChoice.Hand, leftIndex2));
                colliderList.Add(new CapsuleCollider((leftIndex2.position - leftIndex3.position).magnitude / 2 * 0.8f, leftIndex3.position, leftIndex3.position + (leftIndex3.position - leftIndex2.position) * 0.8f, ColliderChoice.Hand, leftIndex3));
                colliderList.Add(new CapsuleCollider((leftMiddle1.position - leftMiddle2.position).magnitude / 2, leftMiddle1.position, leftMiddle2.position, ColliderChoice.Hand, leftMiddle1));
                colliderList.Add(new CapsuleCollider((leftMiddle2.position - leftMiddle3.position).magnitude / 2, leftMiddle2.position, leftMiddle3.position, ColliderChoice.Hand, leftMiddle2));
                colliderList.Add(new CapsuleCollider((leftMiddle2.position - leftMiddle3.position).magnitude / 2 * 0.8f, leftMiddle3.position, leftMiddle3.position + (leftMiddle3.position - leftMiddle2.position) * 0.8f, ColliderChoice.Hand, leftMiddle3));
                colliderList.Add(new CapsuleCollider((leftRing1.position - leftRing2.position).magnitude / 2, leftRing1.position, leftRing2.position, ColliderChoice.Hand, leftRing1));
                colliderList.Add(new CapsuleCollider((leftRing2.position - leftRing3.position).magnitude / 2, leftRing2.position, leftRing3.position, ColliderChoice.Hand, leftRing2));
                colliderList.Add(new CapsuleCollider((leftRing2.position - leftRing3.position).magnitude / 2 * 0.8f, leftRing3.position, leftRing3.position + (leftRing3.position - leftRing2.position) * 0.8f, ColliderChoice.Hand, leftRing3));
                colliderList.Add(new CapsuleCollider((leftLittle1.position - leftLittle2.position).magnitude / 2, leftLittle1.position, leftLittle2.position, ColliderChoice.Hand, leftLittle1));
                colliderList.Add(new CapsuleCollider((leftLittle2.position - leftLittle3.position).magnitude / 2, leftLittle2.position, leftLittle3.position, ColliderChoice.Hand, leftLittle2));
                colliderList.Add(new CapsuleCollider((leftLittle2.position - leftLittle3.position).magnitude / 2 * 0.8f, leftLittle3.position, leftLittle3.position + (leftLittle3.position - leftLittle2.position) * 0.8f, ColliderChoice.Hand, leftLittle3));
                // right
                var rightThumb1 = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                var rightThumb2 = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
                var rightThumb3 = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
                var rightIndex1 = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                var rightIndex2 = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
                var rightIndex3 = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
                var rightMiddle1 = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                var rightMiddle2 = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                var rightMiddle3 = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
                var rightRing1 = animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
                var rightRing2 = animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate);
                var rightRing3 = animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
                var rightLittle1 = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
                var rightLittle2 = animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate);
                var rightLittle3 = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);

                var rightHandLength = (rightMiddle2.position - rightHand.position).magnitude;
                //colliderList.Add(new OBBBoxCollider(rightHandCenterPoint, new Vector3(rightHandLength * 0.25f, rightHandLength, rightHandLength), rightMiddle2.position - rightMiddle1.position, ColliderChoice.Hand, rightHand));

                colliderList.Add(new CapsuleCollider((rightThumb1.position - rightThumb2.position).magnitude / 2, rightThumb1.position, rightThumb2.position, ColliderChoice.Hand, rightThumb1));
                colliderList.Add(new CapsuleCollider((rightThumb2.position - rightThumb3.position).magnitude / 2, rightThumb2.position, rightThumb3.position, ColliderChoice.Hand, rightThumb2));
                colliderList.Add(new CapsuleCollider((rightThumb2.position - rightThumb3.position).magnitude / 2 * 0.8f, rightThumb3.position, rightThumb3.position + (rightThumb3.position- rightThumb2.position ) * 0.8f, ColliderChoice.Hand, rightThumb3));
                colliderList.Add(new CapsuleCollider((rightIndex1.position - rightIndex2.position).magnitude / 2, rightIndex1.position, rightIndex2.position, ColliderChoice.Hand, rightIndex1));
                colliderList.Add(new CapsuleCollider((rightIndex2.position - rightIndex3.position).magnitude / 2, rightIndex2.position, rightIndex3.position, ColliderChoice.Hand, rightIndex2));
                colliderList.Add(new CapsuleCollider((rightIndex2.position - rightIndex3.position).magnitude / 2 * 0.8f, rightIndex3.position, rightIndex3.position + (rightIndex3.position - rightIndex2.position) * 0.8f, ColliderChoice.Hand, rightIndex3));
                colliderList.Add(new CapsuleCollider((rightMiddle1.position - rightMiddle2.position).magnitude / 2, rightMiddle1.position, rightMiddle2.position, ColliderChoice.Hand, rightMiddle1));
                colliderList.Add(new CapsuleCollider((rightMiddle2.position - rightMiddle3.position).magnitude / 2, rightMiddle2.position, rightMiddle3.position, ColliderChoice.Hand, rightMiddle2));
                colliderList.Add(new CapsuleCollider((rightMiddle2.position - rightMiddle3.position).magnitude / 2 * 0.8f, rightMiddle3.position, rightMiddle3.position + (rightMiddle3.position - rightMiddle2.position) * 0.8f, ColliderChoice.Hand, rightMiddle3));
                colliderList.Add(new CapsuleCollider((rightRing1.position - rightRing2.position).magnitude / 2, rightRing1.position, rightRing2.position, ColliderChoice.Hand, rightRing1));
                colliderList.Add(new CapsuleCollider((rightRing2.position - rightRing3.position).magnitude / 2, rightRing2.position, rightRing3.position, ColliderChoice.Hand, rightRing2));
                colliderList.Add(new CapsuleCollider((rightRing2.position - rightRing3.position).magnitude / 2 * 0.8f, rightRing3.position, rightRing3.position + (rightRing3.position - rightRing2.position) * 0.8f, ColliderChoice.Hand, rightRing3));
                colliderList.Add(new CapsuleCollider((rightLittle1.position - rightLittle2.position).magnitude / 2, rightLittle1.position, rightLittle2.position, ColliderChoice.Hand, rightLittle1));
                colliderList.Add(new CapsuleCollider((rightLittle2.position - rightLittle3.position).magnitude / 2, rightLittle2.position, rightLittle3.position, ColliderChoice.Hand, rightLittle2));
                colliderList.Add(new CapsuleCollider((rightLittle2.position - rightLittle3.position).magnitude / 2 * 0.8f, rightLittle3.position, rightLittle3.position + (rightLittle3.position - rightLittle2.position) * 0.8f, ColliderChoice.Hand, rightLittle3));
            }
     



            for (int i = 0; i < colliderList.Count; i++)
            {
                colliderList[i].colliderRead.isConnectWithBody = true;
            }
            runtimeColliders.AddRange(colliderList);
        }
        public static float CheckNearstPointToSegment(float MaxLength, Vector3 position, Vector3 direction,ColliderChoice choice ,List<ADBRuntimePoint> pointTrans)
        {
            if (pointTrans==null||pointTrans.Count == 0)
            {
                return MaxLength;
            }
            for (int i = 0; i < pointTrans.Count; i++)
            {
                if ((pointTrans[i].pointRead.colliderChoice & choice) == 0) 
                    continue;

                if (direction == Vector3.zero)
                {
                    MaxLength = Mathf.Min(MaxLength, (position - pointTrans[i].trans.position).magnitude);
                }
                else
                {
                    Vector3 nearstPoint = position + direction * Mathf.Clamp01(Vector3.Dot(pointTrans[i].trans.position - position, direction) / direction.sqrMagnitude);
                    MaxLength = Mathf.Min(MaxLength, (nearstPoint - pointTrans[i].trans.position).magnitude-0.001f);
                }
            }
            return MaxLength;
        }
    }
}
