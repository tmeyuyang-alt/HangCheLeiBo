using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DeviceSignalNameLibrary", menuName = "Config/Device Signal Name Library")]
public class DeviceSignalNameLibrary : ScriptableObject
{
    public List<string> names = new List<string>
    {
        "\u542f\u52a8\u4fe1\u53f7",
        "\u505c\u6b62\u4fe1\u53f7",
        "\u62c9\u95f8\u4fe1\u53f7"
    };
}
