// Created by 月北(ybwork-cn) https://github.com/ybwork-cn/

using UnityEngine;

public class ECSVertexAnimation : MonoBehaviour
{
    [SerializeField] bool IsLoop;

    Material Material;
    float Duration;
    float time;

    void Start()
    {
        Material = GetComponent<MeshRenderer>().material;
        Duration = Material.GetFloat("_AnimLen");
        time = 0;
    }

    void Update()
    {
        time += Time.deltaTime;
        if (IsLoop)
            time %= Duration;
        Material.SetFloat("_CurrentTime", time);
    }
}
