﻿#define USE_JSITEXTMESH
/*****************************************************************************
 * RasterPropMonitor
 * =================
 * Plugin for Kerbal Space Program
 *
 *  by Mihara (Eugene Medvedev), MOARdV, and other contributors
 * 
 * RasterPropMonitor is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, revision
 * date 29 June 2007, or (at your option) any later version.
 * 
 * RasterPropMonitor is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with RasterPropMonitor.  If not, see <http://www.gnu.org/licenses/>.
 ****************************************************************************/
using System;
using UnityEngine;

namespace JSI
{
    // Note 1: http://docs.unity3d.com/Manual/StyledText.html details the "richText" abilities
    public class JSILabel : InternalModule
    {
        [KSPField]
        public string labelText = "uninitialized";
        [KSPField]
        public string transformName;
        [KSPField]
        public Vector2 transformOffset = Vector2.zero;
        [KSPField]
        public string emissive = "always";
        private EmissiveMode emissiveMode = EmissiveMode.always;
        enum EmissiveMode
        {
            always,
            never,
            active,
            passive
        };

        [KSPField]
        public float fontSize = 8.0f;
        [KSPField]
        public float lineSpacing = 1.0f;
        [KSPField]
        public string fontName = "Arial";
        [KSPField]
        public string anchor = string.Empty;
        [KSPField]
        public string alignment = string.Empty;
        [KSPField]
        public int fontQuality = 32;

        [KSPField]
        public int refreshRate = 10;
        [KSPField]
        public bool oneshot;
        private bool oneshotComplete;
        [KSPField]
        public string variableName = string.Empty;
        [KSPField]
        public string positiveColor = string.Empty;
        private Color positiveColorValue = XKCDColors.White;
        [KSPField]
        public string negativeColor = string.Empty;
        private Color negativeColorValue = XKCDColors.White;
        [KSPField]
        public string zeroColor = string.Empty;
        private Color zeroColorValue = XKCDColors.White;
        private bool variablePositive = false;

#if USE_JSITEXTMESH
        private JSITextMesh textObj;
#else
        private TextMesh textObj;
        private Material overrideMaterial;
#endif
        private Font font;

        private int updateCountdown;
        private Action<RPMVesselComputer, float> del;
        private StringProcessorFormatter spf;

        public void Start()
        {
            try
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);

                Transform textObjTransform = internalProp.FindModelTransform(transformName);
                Vector3 localScale = internalProp.transform.localScale;

                Transform offsetTransform = new GameObject().transform;
                offsetTransform.gameObject.name = "JSILabel-" + this.internalProp.propID+"-" + this.GetHashCode().ToString();
                offsetTransform.gameObject.layer = textObjTransform.gameObject.layer;
                offsetTransform.SetParent(textObjTransform, false);
                offsetTransform.Translate(transformOffset.x* localScale.x, transformOffset.y* localScale.y, 0.0f);
#if USE_JSITEXTMESH
                textObj = offsetTransform.gameObject.AddComponent<JSITextMesh>();
#else
                textObj = offsetTransform.gameObject.AddComponent<TextMesh>();
#endif

                font = JUtil.LoadFont(fontName, fontQuality);

                textObj.font = font;

                if(emissive.ToLower() == EmissiveMode.always.ToString())
                {
                    emissiveMode = EmissiveMode.always;
                }
                else if (emissive.ToLower() == EmissiveMode.never.ToString())
                {
                    emissiveMode = EmissiveMode.never;
                }
                else if (emissive.ToLower() == EmissiveMode.active.ToString())
                {
                    emissiveMode = EmissiveMode.active;
                }
                else if (emissive.ToLower() == EmissiveMode.passive.ToString())
                {
                    emissiveMode = EmissiveMode.passive;
                }
                else
                {
                    JUtil.LogErrorMessage(this, "Unrecognized emissive mode '{0}' in config for {1} ({2})", emissive, internalProp.propID, internalProp.propName);
                    emissiveMode = EmissiveMode.always;
                }

#if USE_JSITEXTMESH
                textObj.material.mainTexture = font.material.mainTexture;
#else
                Renderer r = textObj.GetComponent<Renderer>();
                overrideMaterial = new Material(JUtil.LoadInternalShader("RPM/JSILabel"));
                overrideMaterial.mainTexture = font.material.mainTexture;
                r.material = overrideMaterial;
#endif
                textObj.richText = true;

                if (!string.IsNullOrEmpty(anchor))
                {
                    if(anchor == TextAnchor.LowerCenter.ToString())
                    {
                        textObj.anchor = TextAnchor.LowerCenter;
                    }
                    else if (anchor == TextAnchor.LowerLeft.ToString())
                    {
                        textObj.anchor = TextAnchor.LowerLeft;
                    }
                    else if (anchor == TextAnchor.LowerRight.ToString())
                    {
                        textObj.anchor = TextAnchor.LowerRight;
                    }
                    else if (anchor == TextAnchor.MiddleCenter.ToString())
                    {
                        textObj.anchor = TextAnchor.MiddleCenter;
                    }
                    else if (anchor == TextAnchor.MiddleLeft.ToString())
                    {
                        textObj.anchor = TextAnchor.MiddleLeft;
                    }
                    else if (anchor == TextAnchor.MiddleRight.ToString())
                    {
                        textObj.anchor = TextAnchor.MiddleRight;
                    }
                    else if (anchor == TextAnchor.UpperCenter.ToString())
                    {
                        textObj.anchor = TextAnchor.UpperCenter;
                    }
                    else if (anchor == TextAnchor.UpperLeft.ToString())
                    {
                        textObj.anchor = TextAnchor.UpperLeft;
                    }
                    else if (anchor == TextAnchor.UpperRight.ToString())
                    {
                        textObj.anchor = TextAnchor.UpperRight;
                    }
                    else
                    {
                        JUtil.LogErrorMessage(this, "Unrecognized anchor '{0}' in config for {1} ({2})", anchor, internalProp.propID, internalProp.propName);
                    }
                }

                if (!string.IsNullOrEmpty(alignment))
                {
                    if(alignment == TextAlignment.Center.ToString())
                    {
                        textObj.alignment = TextAlignment.Center;
                    }
                    else if (alignment == TextAlignment.Left.ToString())
                    {
                        textObj.alignment = TextAlignment.Left;
                    }
                    else if (alignment == TextAlignment.Right.ToString())
                    {
                        textObj.alignment = TextAlignment.Right;
                    }
                    else
                    {
                        JUtil.LogErrorMessage(this, "Unrecognized alignment '{0}' in config for {1} ({2})", alignment, internalProp.propID, internalProp.propName);
                    }
                }

                float sizeScalar = 32.0f / (float)font.fontSize;
#if USE_JSITEXTMESH
                textObj.characterSize = fontSize * 0.00005f * sizeScalar;
#else
                textObj.characterSize = fontSize * 0.0005f * sizeScalar;
#endif
                textObj.lineSpacing = textObj.lineSpacing * lineSpacing;

                // Force oneshot if there's no variables:
                oneshot |= !labelText.Contains("$&$");
                string sourceString = labelText.UnMangleConfigText();

                if (!string.IsNullOrEmpty(sourceString) && sourceString.Length > 1)
                {
                    // Alow a " character to escape leading whitespace
                    if (sourceString[0] == '"')
                    {
                        sourceString = sourceString.Substring(1);
                    }
                }
                spf = new StringProcessorFormatter(sourceString);

                if (!oneshot)
                {
                    comp.UpdateDataRefreshRate(refreshRate);
                }

                if (!string.IsNullOrEmpty(zeroColor))
                {
                    zeroColorValue = ConfigNode.ParseColor32(zeroColor);
                    textObj.color = zeroColorValue;
                }

                if (!(string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(positiveColor) || string.IsNullOrEmpty(negativeColor) || string.IsNullOrEmpty(zeroColor)))
                {
                    positiveColorValue = ConfigNode.ParseColor32(positiveColor);
                    negativeColorValue = ConfigNode.ParseColor32(negativeColor);
                    del = (Action<RPMVesselComputer, float>)Delegate.CreateDelegate(typeof(Action<RPMVesselComputer, float>), this, "OnCallback");
                    comp.RegisterCallback(variableName, del);

                    // Initialize the text color.
                    float value = comp.ProcessVariable(variableName).MassageToFloat();
                    if (value < 0.0f)
                    {
                        textObj.color = negativeColorValue;
                        variablePositive = false;
                    }
                    else if (value > 0.0f)
                    {
                        textObj.color = positiveColorValue;
                        variablePositive = true;
                    }
                    else
                    {
                        textObj.color = zeroColorValue;
                        variablePositive = false;
                    }
                }

                UpdateShader();
            }
            catch(Exception e)
            {
                JUtil.LogErrorMessage(this, "Start failed in prop {1} ({2}) with exception {0}", e, internalProp.propID, internalProp.propName);
                spf = new StringProcessorFormatter(string.Empty);
            }
        }

        private void UpdateShader()
        {
            float emissiveValue = 1.0f;
            if(emissiveMode == EmissiveMode.always)
            {
                emissiveValue = 1.0f;
            }
            else if(emissiveMode == EmissiveMode.never)
            {
                emissiveValue = 0.0f;
            }
            else if(variablePositive ^ (emissiveMode==EmissiveMode.passive))
            {
                emissiveValue = 1.0f;
            }
            else
            {
                emissiveValue = 0.0f;
            }

            // TODO: Use an index, not a string.
#if USE_JSITEXTMESH
            textObj.material.SetFloat("_EmissiveFactor", emissiveValue);
#else
            overrideMaterial.SetFloat("_EmissiveFactor", emissiveValue);
#endif
        }

        public void OnDestroy()
        {
            if (del != null)
            {
                try
                {
                    RPMVesselComputer comp = null;
                    if (RPMVesselComputer.TryGetInstance(vessel, ref comp))
                    {
                        comp.UnregisterCallback(variableName, del);
                    }
                }
                catch
                {
                    //JUtil.LogMessage(this, "Trapped exception unregistering JSIVariableLabel (you can ignore this)");
                }
            }
            //JUtil.LogMessage(this, "OnDestroy()");
            Destroy(textObj);
            textObj = null;
#if !USE_JSITEXTMESH
            Destroy(overrideMaterial);
            overrideMaterial = null;
#endif
        }

        private void OnCallback(RPMVesselComputer comp, float value)
        {
            // Sanity checks:
            if (vessel == null || vessel.id != comp.id)
            {
                // We're not attached to a ship?
                comp.UnregisterCallback(variableName, del);
                return;
            }

            if (value < 0.0f)
            {
                textObj.color = negativeColorValue;
                variablePositive = false;
            }
            else if (value > 0.0f)
            {
                textObj.color = positiveColorValue;
                variablePositive = true;
            }
            else
            {
                textObj.color = zeroColorValue;
                variablePositive = false;
            }
        }

        private bool UpdateCheck()
        {
            if (updateCountdown <= 0)
            {
                updateCountdown = refreshRate;
                return true;
            }
            updateCountdown--;
            return false;
        }

        public override void OnUpdate()
        {
            // Update shader parameters
            UpdateShader();

            if (oneshotComplete && oneshot)
            {
                return;
            }

            if (JUtil.RasterPropMonitorShouldUpdate(vessel) && UpdateCheck())
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                textObj.text = StringProcessor.ProcessString(spf, comp);
                oneshotComplete = true;
            }
        }
    }
}
