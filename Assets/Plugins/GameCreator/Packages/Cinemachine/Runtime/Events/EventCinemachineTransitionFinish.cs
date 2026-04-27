using System;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Title("On Cinemachine Transition Finish")]
    [Category("Cinemachine/On Cinemachine Transition Finish")]
    [Description("Executed when the Cinemachine Camera finish transitioning in")]

    [Image(typeof(IconCinemachine), ColorTheme.Type.Green, typeof(OverlayArrowRight))]
    [Keywords("Camera", "Blend", "Change")]
    
    [Serializable]
    public class EventCinemachineTransitionFinish : VisualScripting.Event
    {
        // MEMBERS: -------------------------------------------------------------------------------

        [SerializeField] private CompareGameObjectOrAny m_CinemachineCamera = new CompareGameObjectOrAny(
            true,
            GetGameObjectCinemachine.Create()
        );

        // INITIALIZERS: --------------------------------------------------------------------------
        
        protected override void OnEnable(Trigger trigger)
        {
            base.OnEnable(trigger);
            CinemachineCore.BlendFinishedEvent.AddListener(this.OnCinemachineEvent);
        }
        
        protected override void OnDisable(Trigger trigger)
        {
            base.OnDisable(trigger);
            if (ApplicationManager.IsExiting) return;
            CinemachineCore.BlendFinishedEvent.RemoveListener(this.OnCinemachineEvent);
        }
        
        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void OnCinemachineEvent(ICinemachineMixer mixer, ICinemachineCamera camera)
        {
            if (ApplicationManager.IsExiting) return;
            
            GameObject target = camera is Component component ? component.gameObject : null;
            if (this.m_CinemachineCamera.Match(target, this.m_Trigger.gameObject))
            {
                _ = this.m_Trigger.Execute(this.Self);
            }
        }
    }
}