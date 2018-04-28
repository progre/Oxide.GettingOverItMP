﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluffyUnderware.DevTools.Extensions;
using Oxide.Core;
using Oxide.GettingOverItMP.Components.MapEditing;
using UnityEngine;
using UnityEngine.EventSystems;
using Event = UnityEngine.Event;

namespace Oxide.GettingOverItMP.Components
{
    public class LocalPlayerDebug : MonoBehaviour
    {
        private bool wasLocked;
        private bool showUi;
        private bool lastShowUi;

        private PlayerControl localPlayerControl;
        private MapEditManager mapEditManager;

        private GUIStyle backgroundStyle;

        private int selectedIndex = 0;

        private void Start()
        {
            localPlayerControl = GameObject.Find("Player").GetComponent<PlayerControl>() ?? throw new NotImplementedException("Could not find PlayerControl");
            mapEditManager = GameObject.Find("GOIMP.MapEditManager").GetComponent<MapEditManager>() ?? throw new NotImplementedException("Could not find MapEditManager");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (mapEditManager.EditModeEnabled)
                {
                    Debug.Log("Disabling edit mode");
                    mapEditManager.DisableEditMode();
                }
                else
                {
                    Debug.Log("Enabling edit mode");
                    mapEditManager.EnableEditMode();
                }
            }
        }

        private void OnGUI()
        {
            if (backgroundStyle == null)
            {
                backgroundStyle = new GUIStyle(GUI.skin.box);
                backgroundStyle.normal.background = Texture2D.whiteTexture;
            }

            lastShowUi = showUi;
            showUi = Input.GetKey(KeyCode.F3);

            if (showUi && !lastShowUi)
            {
                wasLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                localPlayerControl.PauseInput(float.MinValue);
            }
            else if (!showUi && lastShowUi && wasLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                localPlayerControl.PauseInput(0);
                selectedIndex = 0;
            }

            if (showUi)
            {
                var currentEvent = Event.current;

                if (currentEvent.isScrollWheel)
                {
                    selectedIndex += currentEvent.delta.y > 0 ? 1 : -1;
                }

                // Raycast world
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D[] raycastHits = Physics2D.RaycastAll(mouseWorldPosition, Vector2.zero, 1000);

                // Raycast UI
                var pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                results = results.Where(result => result.gameObject.transform.childCount == 0).ToList();

                // Clamp selected index to valid a valid index
                selectedIndex = Mathf.Clamp(selectedIndex, 0, results.Count + raycastHits.Length - 1);
                
                // Build string
                StringBuilder builder = new StringBuilder();

                if (raycastHits.Length > 0)
                {
                    builder.Append("World:\n");

                    for (var i = 0; i < raycastHits.Length; ++i)
                    {
                        RaycastHit2D raycastHit = raycastHits[i];

                        if (raycastHit.transform != null)
                        {
                            builder.Append("- ");

                            if (selectedIndex == i)
                                builder.Append("[");

                            builder.Append($"{GetPathString(raycastHit.transform.gameObject)} ({LayerMask.LayerToName(raycastHit.transform.gameObject.layer)})");

                            if (selectedIndex == i)
                                builder.Append("]");

                            if (i < raycastHits.Length - 1)
                                builder.Append("\n");
                        }
                    }
                }

                if (results.Any())
                {
                    builder.AppendLine("\nUI:");

                    for (var i = 0; i < results.Count; i++)
                    {
                        string prefix = selectedIndex - raycastHits.Length == i ? "[" : "";
                        string suffix = selectedIndex - raycastHits.Length == i ? "]" : "";

                        var result = results[i];
                        builder.Append($"- {prefix}{GetPathString(result.gameObject)}{suffix}");

                        if (i < results.Count - 1)
                            builder.AppendLine();
                    }
                }

                if (currentEvent.isKey && currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F4 && (raycastHits.Length > 0 || results.Any()))
                {
                    string toCopy;

                    if (selectedIndex < raycastHits.Length)
                    {
                        toCopy = GetPathString(raycastHits[selectedIndex].transform.gameObject);
                    }
                    else
                    {
                        toCopy = GetPathString(results[selectedIndex - raycastHits.Length].gameObject);
                    }

                    GUIUtility.systemCopyBuffer = toCopy;
                    Interface.Oxide.LogDebug($"Copied to clipboard: {toCopy}");
                }

                if (raycastHits.Length == 0 && !results.Any())
                    return;

                var content = new GUIContent(builder.ToString());
                Vector2 size = GUI.skin.label.CalcSize(content);
                Rect rect = new Rect(Input.mousePosition.x + 10, Screen.height - Input.mousePosition.y + 10, size.x, size.y);
                Rect shadowRect = new Rect(rect);
                shadowRect.x += 1;
                shadowRect.y += 1;
                Rect boxRect = new Rect();
                boxRect.Set(rect.x - 3, rect.y, rect.width + 6, rect.height);

                Color oldBackground = GUI.backgroundColor;

                GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
                GUI.Box(boxRect, "", backgroundStyle);
                GUI.backgroundColor = oldBackground;

                Color oldColor = GUI.color;

                GUI.color = Color.black;
                GUI.Label(shadowRect, content);

                GUI.color = Color.yellow;
                GUI.Label(rect, content);

                GUI.color = oldColor;
            }
        }

        private string GetPathString(GameObject gameObject)
        {
            if (gameObject == null)
                return "null";
            
            var builder = new StringBuilder();

            if (gameObject.transform.parent != null)
                builder.Append(GetPathString(gameObject.transform.parent.gameObject) + "/");

            builder.Append(gameObject.name);

            if (!gameObject.activeInHierarchy)
                builder.Append(" (inactive)");

            return builder.ToString();
        }
    }
}
