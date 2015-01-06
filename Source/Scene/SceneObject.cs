using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;

namespace Inertia
{
    public class SceneObject
    {
        #region Properties
        /// <summary>
        /// User-specified ID of the object
        /// </summary>
        int m_ObjectID;
        public int ObjectID
        {
            get { return m_ObjectID; }
            set { m_ObjectID = value; }
        }

        /// <summary>
        /// Panel id (0 = main panel, extra panels may exist if the user defines multiple directories)
        /// </summary>
        int m_PanelID;
        public int PanelID
        {
            get { return m_PanelID; }
            set { m_PanelID = value; }
        }

        /// <summary>
        /// Start time of the animation.
        /// Animations are interpolated from StartTime to EndTime relative DateTime.Now
        /// </summary>
        DateTime m_StartTime = DateTime.Now;
        public System.DateTime StartTime
        {
            get { return m_StartTime; }
            set { m_StartTime = value; }
        }

        /// <summary>
        /// End time of the animation.
        /// Animations are interpolated from StartTime to EndTime relative DateTime.Now
        /// </summary>
        DateTime m_EndTime = DateTime.Now;
        public System.DateTime EndTime
        {
            get { return m_EndTime; }
            set { m_EndTime = value; }
        }

        /// <summary>
        /// Position and rotational internal variables
        /// </summary>
        public Vector3 p0, p1, p2, p3;
        public Vector3 r0, r1, r2, r3;
        public Vector3 s0, s1, s2, s3;

        /// <summary>
        /// Current 3D position of object
        /// </summary>
        Vector3 m_Position = new Vector3(0, 0, 0);
        public Microsoft.DirectX.Vector3 Position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        /// <summary>
        /// Current 3D rotation of object
        /// </summary>
        Vector3 m_Rotation = new Vector3(0, 0, 0);
        public Microsoft.DirectX.Vector3 Rotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }

        /// <summary>
        /// Current 3D scale of object
        /// </summary>
        Vector3 m_Scale = new Vector3(1, 1, 1);
        public Microsoft.DirectX.Vector3 Scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        /// <summary>
        /// Delegate used to perform the animation
        /// </summary>
        Inertia.Animator.AnimationHandler m_AnimationHandler = null;
        public Inertia.Animator.AnimationHandler AnimationHandler
        {
            get { return m_AnimationHandler; }
            set { m_AnimationHandler = value; }
        }

        /// <summary>
        /// Motion points used to define the animation sequence for this object
        /// </summary>
        AnimationDefinition m_AnimationDefinition = null;
        public AnimationDefinition AnimationDefinition
        {
            get { return m_AnimationDefinition; }
            set { m_AnimationDefinition = value; }
        }

        /// <summary>
        /// Index into the set of motion points that defines the current segment of animation
        /// </summary>
        int m_AnimationSegment = 0;
        public int AnimationSegment
        {
            get { return m_AnimationSegment; }
            set { m_AnimationSegment = value; }
        }

        /// <summary>
        /// Used to identify the final frame of animation during the current tick
        /// </summary>
        bool m_AnimationLastFrame = false;
        public bool AnimationLastFrame
        {
            get { return m_AnimationLastFrame; }
            set { m_AnimationLastFrame = value; }
        }

        /// <summary>
        /// Returns the current scalar time value of the object's animation relative to DateTime.Now
        /// </summary>
        public float Time
        {
            get
            {
                DateTime curTime = DateTime.Now;
                TimeSpan aniSpan = EndTime - StartTime;
                TimeSpan curSpan = curTime - StartTime;
                return (float)(curSpan.TotalMilliseconds / aniSpan.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Returns the clamped Time
        /// </summary>
        public float ClampedTime
        {
            get
            {
                return Math.Max(0, Math.Min(Time, 1));
            }
        }
        #endregion

        public void Init(int objectID, int panelID)
        {
            // Initialize the object ID
            ObjectID = objectID;
            PanelID = panelID;
        }

        public bool StartNextAnimationSegment()
        {
            return StartAnimationSequence(m_AnimationSegment + 1);
        }

        public bool StartAnimationSequence(AnimationDefinition ad)
        {
            if (ad == null) return false;

            AnimationDefinition = ad;
            return StartAnimationSequence(0);
        }

        public bool StartAnimationSequence(int segment)
        {
            // Beyond the range?
            if (segment >= AnimationDefinition.MotionPoints.Count) return false;

            // Set the current segment
            m_AnimationSegment = segment;

            // Parse our animation values
            AnimationDefinition.MotionPoints[m_AnimationSegment].ParsePositions(Position, out p0, out p1, out p2, out p3);
            AnimationDefinition.MotionPoints[m_AnimationSegment].ParseRotations(Rotation, out r0, out r1, out r2, out r3);
            AnimationDefinition.MotionPoints[m_AnimationSegment].ParseScales(Scale, out s0, out s1, out s2, out s3);

            // Set the animation handler
            switch (AnimationDefinition.MotionPoints[m_AnimationSegment].TimeAdjust)
            {
                case AnimationDefinition.ETimeAdjust.Decelerated:
                    AnimationHandler = Animator.DeceleratedBezierAnimator;
                    break;

                case AnimationDefinition.ETimeAdjust.Accelerated:
                    AnimationHandler = Animator.AceleratedBezierAnimator;
                    break;

                default:
                    AnimationHandler = Animator.LinearBezierAnimator;
                    break;
            }

            // Set the animation duration
            StartTime = DateTime.Now;
            EndTime = StartTime.AddMilliseconds(AnimationDefinition.MotionPoints[m_AnimationSegment].DurationMS);
            AnimationLastFrame = false;

            return true;
        }

        public bool Tick()
        {
            // Do we have an animation definition?
            if (AnimationDefinition == null) return false;

            // We need a handler
            if (AnimationHandler == null) return false;

            // Run the handler (returns true if there is more animation to go)
            if (AnimationHandler(this)) return true;

            // We must have reached the end of this segment. Go to the next segment (or signal the
            // end of the animation altogether)
            bool result = StartNextAnimationSegment();

            // Set the last frame flag
            if (!result)
            {
                AnimationLastFrame = true;
            }

            return result;
        }

    }
}
