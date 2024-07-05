using NikkeViewerEX.Core;
using NikkeViewerEX.Serialization;
using UnityEngine;

namespace NikkeViewerEX.Components
{
    public abstract class NikkeViewerBase : MonoBehaviour
    {
        [SerializeField]
        Nikke m_NikkeData;

        public Nikke NikkeData
        {
            get => m_NikkeData;
            set => m_NikkeData = value;
        }

        MainControl mainControl => FindObjectsByType<MainControl>(FindObjectsSortMode.None)[0];
        public MainControl MainControl
        {
            get => mainControl;
        }
    }
}
