using System;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Title("On Cinemachine Deactivate")]
    [Category("Cinemachine/On Cinemachine Deactivate")]
    [Description("Executed when the Cinemachine Camera component is deactivated")]
    
    [Image(typeof(IconCinemachine), ColorTheme.Type.TextLight, typeof(OverlayCross))]
    [Keywords("Camera", "Blend", "Change")]
    
    [Serializable]
    public class EventCinemachineDeactivate : VisualScripting.Event
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
            CinemachineCore.CameraDeactivatedEvent.AddListener(this.OnCinemachineEvent);
        }
        
        protected override void OnDisable(Trigger trigger)
        {
            base.OnDisable(trigger);
            if (ApplicationManager.IsExiting) return;
            CinemachineCore.CameraDeactivatedEvent.RemoveListener(this.OnCinemachineEvent);
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