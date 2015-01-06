using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;

namespace Inertia
{
    public class Scene
    {
        /// <summary>
        /// Objects in the scene
        /// </summary>
        static List<SceneObject> m_Objects = new List<SceneObject>();
        public static List<SceneObject> Objects
        {
            get { return m_Objects; }
        }

        #region List manipulation
        /// <summary>
        /// Add an object to the scene
        /// </summary>
        /// <param name="objectID"></param>
        static public bool Add(int objectID, int panelID)
        {
            // If this panel already exists, bail
            foreach(SceneObject so in Scene.Objects)
            {
                if (so.ObjectID == objectID && so.PanelID == panelID)
                {
                    return false;
                }
            }

            SceneObject obj = new SceneObject();

            // Init the object
            obj.Init(objectID, panelID);
            
            // Add the object to our list
            Objects.Add(obj);

            // Trigger the GameActivate event for the new panel
            Animator.TriggerGlobalEvent("GameActivate", panelID);

            return true;
        }

        static public bool Add(Game game, int gameID)
        {
            bool result = false;
            for (int i = 0; i < game.Panels.Count; ++i)
            {
                // !HACK! - this prevents info panels from being drawn
                //if (game.Panels[i].IsTextPanel()) continue;

                if (Scene.Add(gameID, i))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds an object that matches a given ID
        /// </summary>
        /// <param name="id">ID to match</param>
        /// <returns>Matching object or null if not found</returns>
        public static SceneObject Find(int id)
        {
            foreach (SceneObject obj in Objects)
            {
                if (obj.ObjectID == id) return obj;
            }

            return null;
        }
        #endregion
    }
}
