using System;
using System.Threading.Tasks;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Version(0, 0, 1)]
    
    [Title("Cinemachine Target")]
    [Category("Cinemachine/Cinemachine Target")]
    
    [Description("Changes the Target of a Cinemachine Camera instance")]

    [Keywords("Camera", "Virtual")]
    [Image(typeof(IconCinemachine), ColorTheme.Type.Blue)]
    
    [Serializable]
    public class InstructionCinemachineTarget : Instruction
    {
        [SerializeField]
        private PropertyGetGameObject m_CinemachineCamera = GetGameObjectCinemachine.Create();

        [SerializeField] private PropertyGetGameObject m_Target = new PropertyGetGameObject();
        
        // PROPERTIES: ----------------------------------------------------------------------------
        
        public override string Title => $"Target {this.m_CinemachineCamera} = {this.m_Target}";
        
        // RUN METHOD: ----------------------------------------------------------------------------

        protected override Task Run(Args args)
        {
            CinemachineCamera cinemachineCamera = this.m_CinemachineCamera.Get<CinemachineCamera>(args);
            if (cinemachineCamera == null) return DefaultResult;
            
            Transform target = this.m_Target.Get<Transform>(args);

            cinemachineCamera.Target = new CameraTarget
            {
                TrackingTarget = target,
                CustomLookAtTarget = false
            };
            
            return DefaultResult;
        }
    }
}