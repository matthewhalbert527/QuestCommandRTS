using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

namespace QuestCommandRTS
{
    public sealed class RtsInputController : MonoBehaviour
    {
        private readonly List<InputDevice> xrDevices = new List<InputDevice>();
        private readonly List<RtsUnit> commandUnits = new List<RtsUnit>();
        private readonly List<RtsEntity>[] controlGroups = new List<RtsEntity>[5];
        private RtsGame game;
        private bool lastTrigger;
        private bool lastGrip;
        private bool lastPrimary;
        private bool lastSecondary;
        private bool mouseSelectionActive;
        private bool mouseDragging;
        private Vector2 mouseSelectionStart;
        private Rect mouseSelectionRect;
        private const float DragThreshold = 8f;

        public void Initialize(RtsGame owner)
        {
            game = owner;
            for (int i = 0; i < controlGroups.Length; i++)
            {
                controlGroups[i] = new List<RtsEntity>();
            }
        }

        private void Update()
        {
            if (game == null || game.CommandCamera == null)
            {
                return;
            }

            HandleCameraMovement();
            HandleControlGroups();

            Ray ray = GetPointerRay();
            ReadButtons(out bool selectDown, out bool commandDown, out bool cancelDown);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cancelDown = true;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                game.SelectCombatUnits();
            }

            if (game.IsMatchOver)
            {
                return;
            }

            HandleBuildHotkeys();

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
                game.BuildManager.UpdatePlacement(ray);

                if ((selectDown || Input.GetMouseButtonDown(0)) && !IsPointerOverUi())
                {
                    game.BuildManager.TryConfirmPlacement();
                }

                if (commandDown || cancelDown)
                {
                    game.BuildManager.CancelPlacement();
                }

                return;
            }

            if (cancelDown)
            {
                game.ClearSelection();
                return;
            }

            if (HandleMouseSelection(ray))
            {
                return;
            }

            if (selectDown && !IsPointerOverUi())
            {
                SelectFromRay(ray);
            }

            if (commandDown && !IsPointerOverUi())
            {
                CommandFromRay(ray);
            }
        }

        private void OnGUI()
        {
            if (!mouseSelectionActive || !mouseDragging)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.35f, 0.8f, 1f, 0.18f);
            GUI.DrawTexture(mouseSelectionRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.55f, 0.95f, 1f, 0.9f);
            DrawRectOutline(mouseSelectionRect, 2f);
            GUI.color = previousColor;
        }

        private bool HandleMouseSelection(Ray ray)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi())
                {
                    return true;
                }

                mouseSelectionActive = true;
                mouseDragging = false;
                mouseSelectionStart = Input.mousePosition;
                mouseSelectionRect = new Rect(mouseSelectionStart.x, Screen.height - mouseSelectionStart.y, 0f, 0f);
                return true;
            }

            if (!mouseSelectionActive)
            {
                return false;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 current = Input.mousePosition;
                mouseDragging = mouseDragging || Vector2.Distance(mouseSelectionStart, current) > DragThreshold;
                mouseSelectionRect = GetScreenRect(mouseSelectionStart, current);
                return true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (mouseDragging)
                {
                    game.SelectPlayerEntitiesInScreenRect(mouseSelectionRect, additive);
                }
                else
                {
                    SelectFromRay(ray);
                }

                mouseSelectionActive = false;
                mouseDragging = false;
                return true;
            }

            return true;
        }

        private void SelectFromRay(Ray ray)
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!Physics.Raycast(ray, out RaycastHit hit, 250f))
            {
                if (!additive)
                {
                    game.ClearSelection();
                }

                return;
            }

            RtsEntity entity = hit.collider.GetComponentInParent<RtsEntity>();
            game.SelectEntity(entity, additive);
        }

        private void CommandFromRay(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, 250f))
            {
                return;
            }

            RtsEntity entity = hit.collider.GetComponentInParent<RtsEntity>();
            if (entity != null && entity.Team == RtsTeam.Enemy)
            {
                IssueAttack(entity);
                return;
            }

            ResourceNode resource = hit.collider.GetComponentInParent<ResourceNode>();
            if (resource != null)
            {
                IssueHarvest(resource);
                return;
            }

            Vector3 point = hit.point;
            point.y = 0f;
            if (TrySetRallyPoint(point))
            {
                return;
            }

            IssueMove(point);
        }

        private void IssueAttack(RtsEntity target)
        {
            GatherSelectedUnits();
            for (int i = 0; i < commandUnits.Count; i++)
            {
                commandUnits[i].IssueAttack(target);
            }
        }

        private void IssueHarvest(ResourceNode resource)
        {
            RefineryStructure refinery = game.FindNearestPlayerRefinery(resource.transform.position);
            GatherSelectedUnits();

            bool assigned = false;
            for (int i = 0; i < commandUnits.Count; i++)
            {
                HarvesterUnit harvester = commandUnits[i] as HarvesterUnit;
                if (harvester != null)
                {
                    harvester.IssueHarvest(resource, refinery);
                    assigned = true;
                }
            }

            if (!assigned)
            {
                game.SpawnFloatingText("Select harvester", resource.transform.position + Vector3.up * 2f, Color.yellow);
            }
        }

        private void IssueMove(Vector3 point)
        {
            GatherSelectedUnits();
            int count = commandUnits.Count;

            for (int i = 0; i < count; i++)
            {
                commandUnits[i].IssueMove(point + FormationOffset(i, count));
            }
        }

        private bool TrySetRallyPoint(Vector3 point)
        {
            bool hasSelectedUnit = false;
            bool setAny = false;

            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsEntity entity = game.Selection[i];
                if (entity is RtsUnit && entity.Team == RtsTeam.Player)
                {
                    hasSelectedUnit = true;
                    break;
                }
            }

            if (hasSelectedUnit)
            {
                return false;
            }

            for (int i = 0; i < game.Selection.Count; i++)
            {
                ProductionStructure producer = game.Selection[i] as ProductionStructure;
                if (producer != null && producer.Team == RtsTeam.Player)
                {
                    producer.SetRallyPoint(point);
                    setAny = true;
                }
            }

            if (setAny)
            {
                game.SpawnFloatingText("Rally set", point + Vector3.up * 1.4f, new Color(0.5f, 0.95f, 1f));
            }

            return setAny;
        }

        private void GatherSelectedUnits()
        {
            commandUnits.Clear();

            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsUnit unit = game.Selection[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player)
                {
                    commandUnits.Add(unit);
                }
            }
        }

        private void HandleBuildHotkeys()
        {
            if (game.BuildManager == null || game.BuildManager.IsPlacing)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                game.TryQueueUnit(UnitKind.Rifleman);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                game.TryQueueUnit(UnitKind.Harvester);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                game.TryQueueUnit(UnitKind.Tank);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                game.BuildManager.BeginPlacement(StructureKind.PowerPlant);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                game.BuildManager.BeginPlacement(StructureKind.Barracks);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                game.BuildManager.BeginPlacement(StructureKind.Refinery);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                game.BuildManager.BeginPlacement(StructureKind.WarFactory);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                game.BuildManager.BeginPlacement(StructureKind.Turret);
            }
        }

        private void HandleControlGroups()
        {
            for (int i = 0; i < controlGroups.Length; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha5 + i);
                if (!Input.GetKeyDown(key))
                {
                    continue;
                }

                bool assign = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (assign)
                {
                    AssignControlGroup(i);
                }
                else
                {
                    SelectControlGroup(i);
                }
            }
        }

        private void AssignControlGroup(int index)
        {
            List<RtsEntity> group = controlGroups[index];
            group.Clear();

            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsEntity entity = game.Selection[i];
                if (entity != null && entity.Team == RtsTeam.Player && entity.IsAlive)
                {
                    group.Add(entity);
                }
            }

            if (group.Count > 0)
            {
                game.SpawnFloatingText("Group " + (index + 5), game.GetPlayerBaseCenter() + Vector3.up * 2.8f, Color.white);
            }
        }

        private void SelectControlGroup(int index)
        {
            List<RtsEntity> group = controlGroups[index];
            game.ClearSelection();

            for (int i = group.Count - 1; i >= 0; i--)
            {
                RtsEntity entity = group[i];
                if (entity == null || !entity.IsAlive || entity.Team != RtsTeam.Player)
                {
                    group.RemoveAt(i);
                    continue;
                }

                game.SelectEntity(entity, true);
            }
        }

        private void HandleCameraMovement()
        {
            Transform cameraTransform = game.CommandCamera.transform;
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                move += Vector3.forward;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                move += Vector3.back;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                move += Vector3.left;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                move += Vector3.right;
            }

            if (!IsPointerOverUi())
            {
                const float edge = 18f;
                Vector3 mouse = Input.mousePosition;
                if (mouse.x >= 0f && mouse.x <= Screen.width && mouse.y >= 0f && mouse.y <= Screen.height)
                {
                    if (mouse.x <= edge)
                    {
                        move += Vector3.left;
                    }
                    else if (mouse.x >= Screen.width - edge)
                    {
                        move += Vector3.right;
                    }

                    if (mouse.y <= edge)
                    {
                        move += Vector3.back;
                    }
                    else if (mouse.y >= Screen.height - edge)
                    {
                        move += Vector3.forward;
                    }
                }
            }

            Vector3 position = cameraTransform.position;
            if (move.sqrMagnitude > 0.01f)
            {
                float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 25f : 15f;
                position += move.normalized * speed * Time.deltaTime;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                position += cameraTransform.forward * scroll * 3.2f;
            }

            position.y = Mathf.Clamp(position.y, 16f, 42f);
            float cameraLimit = RtsBalance.MapHalfSize - 6f;
            position.x = Mathf.Clamp(position.x, -cameraLimit, cameraLimit);
            position.z = Mathf.Clamp(position.z, -cameraLimit - 8f, cameraLimit);
            cameraTransform.position = position;
        }

        private Ray GetPointerRay()
        {
            if (TryGetXrRay(out Ray xrRay))
            {
                return xrRay;
            }

            return game.CommandCamera.ScreenPointToRay(Input.mousePosition);
        }

        private bool TryGetXrRay(out Ray ray)
        {
            xrDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right, xrDevices);

            if (xrDevices.Count == 0)
            {
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left, xrDevices);
            }

            for (int i = 0; i < xrDevices.Count; i++)
            {
                InputDevice device = xrDevices[i];
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                    device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                {
                    ray = new Ray(position, rotation * Vector3.forward);
                    return true;
                }
            }

            ray = default(Ray);
            return false;
        }

        private void ReadButtons(out bool selectDown, out bool commandDown, out bool cancelDown)
        {
            bool trigger = ReadXrButton(CommonUsages.triggerButton);
            bool grip = ReadXrButton(CommonUsages.gripButton);
            bool primary = ReadXrButton(CommonUsages.primaryButton);
            bool secondary = ReadXrButton(CommonUsages.secondaryButton);

            selectDown = Input.GetMouseButtonDown(0) || (trigger && !lastTrigger);
            commandDown = Input.GetMouseButtonDown(1) || (grip && !lastGrip) || (primary && !lastPrimary);
            cancelDown = Input.GetMouseButtonDown(2) || (secondary && !lastSecondary);

            lastTrigger = trigger;
            lastGrip = grip;
            lastPrimary = primary;
            lastSecondary = secondary;
        }

        private bool ReadXrButton(InputFeatureUsage<bool> usage)
        {
            xrDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, xrDevices);

            for (int i = 0; i < xrDevices.Count; i++)
            {
                if (xrDevices[i].TryGetFeatureValue(usage, out bool pressed) && pressed)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 FormationOffset(int index, int count)
        {
            int width = Mathf.Min(4, Mathf.Max(1, count));
            int row = index / width;
            int column = index % width;
            float center = (width - 1) * 0.5f;
            return new Vector3((column - center) * 1.45f, 0f, row * -1.45f);
        }

        private static Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;
            Vector2 topLeft = Vector2.Min(start, end);
            Vector2 bottomRight = Vector2.Max(start, end);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private static void DrawRectOutline(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
