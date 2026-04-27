using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Version(0, 0, 1)]
    
    [Title("Cinemachine Prioritize")]
    [Category("Cinemachine/Cinemachine Prioritize")]
    
    [Description("Prioritizes a Cinemachine camera for all those with the same priority")]

    [Keywords("Camera", "Virtual")]
    [Image(typeof(IconCinemachine), ColorTheme.Type.Blue)]
    
    [Serializable]
    public class InstructionCinemachinePrioritize : Instruction
    {
        [SerializeField]
        private PropertyGetGameObject m_CinemachineCamera = GetGameObjectCinemachine.Create();
        
        // PROPERTIES: ----------------------------------------------------------------------------
        
        public override string Title => $"Prioritize {this.m_CinemachineCamera}";
        
        // RUN METHOD: ----------------------------------------------------------------------------

        protected override Task Run(Args args)
        {
            CinemachineCamera cinemachineCamera = this.m_CinemachineCamera.Get<CinemachineCamera>(args);
            if (cinemachineCamera == null) return DefaultResult;
            
            cinemachineCamera.Prioritize();
            return DefaultResult;
        }
    }
}