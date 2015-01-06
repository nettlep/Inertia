using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using Microsoft.DirectX;

namespace Inertia
{
    public partial class Animator
    {
        /// <summary>
        /// Delegate for updating the animation of an object
        /// </summary>
        /// <param name="info"></param>
        /// <returns>True if the animation has completed</returns>
        public delegate bool AnimationHandler(SceneObject info);

        /// <summary>
        /// Random number generator
        /// </summary>
        static Random m_Rand = new Random();
        public static System.Random Rand
        {
            get { return m_Rand; }
            set { m_Rand = value; }
        }

        /// <summary>
        /// Animation definitions, serialized from motions.xml
        /// </summary>
        static AnimationDefinition[] m_AnimationDefinitions;
        public static AnimationDefinition[] AnimationDefinitions
        {
            get { return m_AnimationDefinitions; }
            set { m_AnimationDefinitions = value; }
        }

        public Animator()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AnimationDefinition[]));
            TextReader textReader = new StreamReader("motions.xml");
            AnimationDefinitions = (AnimationDefinition[]) serializer.Deserialize(textReader);
            textReader.Close();

#if false
            TextWriter textWriter = new StreamWriter(@"C:\users\paul\desktop\zzzz.xml");
            serializer.Serialize(textWriter, AnimationDefinitions);
            textWriter.Close();
#endif
        }

        #region Interpolators
        /// <summary>
        /// Solve for a single value interpolated linearly
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">End point</param>
        /// <param name="t">time</param>
        /// <returns>Value in time, interpolated linearly</returns>
        static public float InterpLinear(float p0, float p1, float t)
        {
            return p0 + t * (p1 - p0);
        }

        /// <summary>
        /// Solve for a 3-dimensional point interpolated linearly
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">End point</param>
        /// <param name="t">time</param>
        /// <returns>Value in time, interpolated linearly</returns>
        static public Vector3 InterpLinear(Vector3 p0, Vector3 p1, float t)
        {
            Vector3 result;
            result.X = InterpLinear(p0.X, p1.X, t);
            result.Y = InterpLinear(p0.Y, p1.Y, t);
            result.Z = InterpLinear(p0.Z, p1.Z, t);
            return result;
        }

        /// <summary>
        /// Solve a single value interpolated along a Bezier curve
        /// Code for this came from http://www.math.ucla.edu/~baker/java/hoefer/Bezier.htm
        /// </summary>
        /// <param name="p0">First point (of four)</param>
        /// <param name="p1">Second point (of four)</param>
        /// <param name="p2">Third point (of four)</param>
        /// <param name="p3">Fourth point (of four)</param>
        /// <param name="t">time</param>
        /// <returns>Component along curve at given time</returns>
        static public float InterpBezier(float p0, float p1, float p2, float p3, float t)
        {
            return
                (p0 + t * (-p0 * 3 + t * (3 * p0 - p0 * t))) +
                t * (3 * p1 + t * (-6 * p1 + p1 * 3 * t)) +
                t * t * (p2 * 3 - p2 * 3 * t) +
                t * t * t * p3;
        }

        /// <summary>
        /// Solve a 3-dimensional point interpolated along a Bezier curve
        /// </summary>
        /// <param name="p0">First point (of four)</param>
        /// <param name="p1">Second point (of four)</param>
        /// <param name="p2">Third point (of four)</param>
        /// <param name="p3">Fourth point (of four)</param>
        /// <param name="t">time</param>
        /// <returns>Point along curve at given time</returns>
        static public Vector3 InterpBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 result;
            result.X = InterpBezier(p0.X, p1.X, p2.X, p3.X, t);
            result.Y = InterpBezier(p0.Y, p1.Y, p2.Y, p3.Y, t);
            result.Z = InterpBezier(p0.Z, p1.Z, p2.Z, p3.Z, t);
            return result;
        }

        /// <summary>
        /// Interpolate a value that decelerates to the destination
        /// </summary>
        static public float InterpDecelerate(float t)
        {
            return 1 - (t-1) * (t-1);
        }

        /// <summary>
        /// Interpolate a value that accelerates to the destination
        /// </summary>
        static public float InterpAccelerate(float t)
        {
            return t * t;
        }
        #endregion

        #region Animation Handlers
        /// <summary>
        /// Generate the animation point in time (decelerated timescale)
        /// </summary>
        /// <param name="obj">Object to animate</param>
        /// <returns>False if we have reached the end of the animation</returns>
        public static bool DeceleratedBezierAnimator(SceneObject obj)
        {
            float t = InterpDecelerate(obj.ClampedTime);
            obj.Position = InterpBezier(obj.p0, obj.p1, obj.p2, obj.p3, t);
            obj.Rotation = InterpBezier(obj.r0, obj.r1, obj.r2, obj.r3, t);
            obj.Scale = InterpBezier(obj.s0, obj.s1, obj.s2, obj.s3, t);
            return t != 1;
        }

        /// <summary>
        /// Generate the animation point in time (accelerated timescale)
        /// </summary>
        /// <param name="obj">Object to animate</param>
        /// <returns>False if we have reached the end of the animation</returns>
        public static bool AceleratedBezierAnimator(SceneObject obj)
        {
            float t = InterpAccelerate(obj.ClampedTime);
            obj.Position = InterpBezier(obj.p0, obj.p1, obj.p2, obj.p3, t);
            obj.Rotation = InterpBezier(obj.r0, obj.r1, obj.r2, obj.r3, t);
            obj.Scale = InterpBezier(obj.s0, obj.s1, obj.s2, obj.s3, t);
            return t != 1;
        }

        /// <summary>
        /// Generate the animation point in time (linear timescale)
        /// </summary>
        /// <param name="obj">Object to animate</param>
        /// <returns>False if we have reached the end of the animation</returns>
        public static bool LinearBezierAnimator(SceneObject obj)
        {
            float t = obj.ClampedTime;
            obj.Position = InterpBezier(obj.p0, obj.p1, obj.p2, obj.p3, t);
            obj.Rotation = InterpBezier(obj.r0, obj.r1, obj.r2, obj.r3, t);
            obj.Scale = InterpBezier(obj.s0, obj.s1, obj.s2, obj.s3, t);
            return t != 1;
        }
        #endregion

        #region Event management
        public static AnimationDefinition FindAnimationDefinitionEntryEvent(string evtName, int panel)
        {
            foreach (AnimationDefinition ad in AnimationDefinitions)
            {
                if (ad.ContainsEntryEvent(evtName) && ad.ContainsPanel(panel))
                {
                    return ad;
                }
            }

            return null;
        }

        public static AnimationDefinition FindAnimationDefinitionExitEvent(AnimationDefinition def, string evtName, int panel)
        {
            // Find the event handler
            string eventHandler = def.GetExitEventHandler(evtName);
            if (eventHandler.Length == 0) return null;

            foreach (AnimationDefinition ad in AnimationDefinitions)
            {
                if (ad.Name.ToLower() == eventHandler && ad.ContainsPanel(panel))
                {
                    return ad;
                }
            }

            return null;
        }

        public static AnimationDefinition FindAnimationDefinition(string name, int panelID)
        {
            foreach (AnimationDefinition ad in AnimationDefinitions)
            {
                if (ad.Name.ToLower() == name.ToLower() && ad.ContainsPanel(panelID))
                {
                    return ad;
                }
            }

            return null;
        }

        public static void TriggerGlobalEvent(string eventName)
        {
            TriggerGlobalEvent(eventName, -1);
        }

        public static void TriggerGlobalEvent(string eventName, int panelID)
        {
            // Scan for any objects that don't have an AnimationDefinition, but match the given event
            foreach (SceneObject obj in Scene.Objects)
            {
                // Match the panel ID
                if (panelID == -1 || panelID == obj.PanelID)
                {
                    // Trigger entry events all for objects that don't have an AnimationDefinition
                    if (obj.AnimationDefinition == null)
                    {
                        // Find a matching AnimationDefinition for this event
                        AnimationDefinition ad = FindAnimationDefinitionEntryEvent(eventName, obj.PanelID);
                        if (ad != null)
                        {
                            // Start that sequence
                            if (obj.StartAnimationSequence(ad))
                            {
                                continue;
                            }
                        }
                    }

                    // Does our current animation request an exit for this event?
                    if (obj.AnimationDefinition != null && obj.AnimationDefinition.ContainsExitEvent(eventName))
                    {
                        string exitEventHandler = obj.AnimationDefinition.GetExitEventHandler(eventName);
                        if (exitEventHandler.Length != 0)
                        {
                            AnimationDefinition ad = FindAnimationDefinition(exitEventHandler, obj.PanelID);
                            if (ad != null)
                            {
                                if (obj.StartAnimationSequence(ad)) continue;
                            }
                        }

                        // There is a null exit-event handler, which means this object goes away
                        obj.AnimationDefinition = null;
                    }
                }
            }

            // Cleanup any objects that have NULL AnimationDefinitions
            DeleteCompletedObjects();
        }

        public static void DeleteCompletedObjects()
        {
            // Advance all completed animations to their next animation segment, next event, or
            // delete the animations which are completed entirely
            bool done = false;
            while (!done)
            {
                done = true;
                foreach (SceneObject obj in Scene.Objects)
                {
                    // We can only animate objects with an animation definition
                    if (obj.AnimationDefinition == null)
                    {
                        // No more animations - this sucker is done. Remove it.
                        Scene.Objects.Remove(obj);
                        done = false;
                        break;
                    }
                }
            }
        }
        #endregion

        #region Event processing
        public static void Tick()
        {
            // Advance all completed animations to their next animation segment, next event, or
            // delete the animations which are completed entirely
            foreach (SceneObject obj in Scene.Objects)
            {
                // Tick the object
                obj.Tick();

                // We can only animate objects with an animation definition
                if (obj.AnimationDefinition != null)
                {
                    // We're only interested in animations which have completed
                    if (!obj.AnimationLastFrame) continue;

                    // Try to advance to the next animation segment
                    if (obj.StartNextAnimationSegment())
                    {
                        continue;
                    }

                    // No more animation segments, advance to the next animation
                    if (obj.AnimationDefinition.NextAnimation.Length != 0)
                    {
                        AnimationDefinition ad = FindAnimationDefinition(obj.AnimationDefinition.NextAnimation, obj.PanelID);

                        if (ad != null)
                        {
                            if (obj.StartAnimationSequence(ad))
                            {
                                continue;
                            }
                        }
                    }

                    // Set this animation as complete
                    obj.AnimationDefinition = null;
                }
            }

            // Cleanup any objects that have NULL AnimationDefinitions
            DeleteCompletedObjects();
        }
        #endregion
    }
}
