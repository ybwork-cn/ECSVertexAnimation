// Created by 月北(ybwork-cn) https://github.com/ybwork-cn/

using System.Collections.Generic;
using UnityEngine;

namespace ybwork.ECSVertexAnimation
{
    /// <summary>
    /// 烘焙器
    /// </summary>
    [ExecuteAlways]
    public class VertexAnimationMapCreator : MonoBehaviour
    {
        public List<Material> Materials;
        public const string SubPath = "VertexAnimationPrefabs";
    }
}