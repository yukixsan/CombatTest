using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Version(0, 0, 1)]
    
    [Title("Change Cinemachine Priority")]
    [Category("Cinemachine/Change Cinemachine Priority")]
    
    [Description("Changes the Priority value of a Cinemachine camera")]

    [Keywords("Camera", "Virtual")]
    [Image(typeof(IconCinemachine), ColorTheme.Type.Blue)]
    
    [Serializable]
    public class InstructionCinemachinePriority : Instruction
    {
        [SerializeField]
        private PropertyGetGameObject m_CinemachineCamera = GetGameObjectCinemachine.Create();
        
        [SerializeField]
        private PropertyGetInteger m_Priority = new PropertyGetInteger(1);
        
        // PROPERTIES: ----------------------------------------------------------------------------

        public override string Title => $"Priority {this.m_CinemachineCamera} = {this.m_Priority}";

        // RUN METHOD: ----------------------------------------------------------------------------

        protected override Task Run(Args args)
        {
            CinemachineCamera cinemachineCamera = this.m_CinemachineCamera.Get<CinemachineCamera>(args);
            if (cinemachineCamera == null) return DefaultResult;
            
            int priority = (int) this.m_Priority.Get(args);
            cinemachineCamera.Priority = priority;
            
            return DefaultResult;
        }
    }
}