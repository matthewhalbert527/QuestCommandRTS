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
        private RtsGame game;
        private RtsCommandPreview commandPreview;
        private bool lastTrigger;
        private bool lastGrip;
        private bool lastPrimary;

        public void Initialize(RtsGame owner)
        {
            game = owner;
        }

        private void Update()
        {
            if (game == null || game.CommandCamera == null)
            {
                return;
            }

            if (RtsHud.BlocksGameInput)
            {
                HideCommandPreview();
                return;
            }

            Ray ray = GetPointerRay();
            ReadButtons(out bool selectDown, out bool commandDown, out bool cancelDown, out bool guardHeld);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cancelDown = true;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                game.SelectCombatUnits();
            }

            HandleBuildHotkeys();

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
                HideCommandPreview();
                game.BuildManager.UpdatePlacement(ray);

                if (selectDown && !IsPointerOverUi())
                {
                    game.BuildManager.TryConfirmPlacement();
                }

                if (commandDown || cancelDown)
                {
                    game.BuildManager.CancelPlacement();
                }

                return;
            }

            UpdateCommandPreview(ray, guardHeld);

            if (cancelDown)
            {
                game.ClearSelection();
                return;
            }

            if (selectDown && !IsPointerOverUi())
            {
                SelectFromRay(ray);
            }

            if (commandDown && !IsPointerOverUi())
            {
                CommandFromRay(ray, guardHeld);
            }
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

            RtsEntity entity = hit.collider != null ? hit.collider.GetComponentInParent<RtsEntity>() : null;
            game.SelectEntity(entity, additive);

            RtsUnit selectedUnit = entity as RtsUnit;
            if (selectedUnit != null && selectedUnit.Team == RtsTeam.Player && game.Audio != null)
            {
                game.Audio.PlayUnitSelected(selectedUnit.UnitKind);
            }
        }

        private void CommandFromRay(Ray ray, bool guardHeld)
        {
            if (!TryGetCommandHit(ray, out RaycastHit hit, out Vector3 point))
            {
                return;
            }

            RtsEntity entity = hit.collider != null ? hit.collider.GetComponentInParent<RtsEntity>() : null;
            if (guardHeld)
            {
                if (entity != null && entity.Team == RtsTeam.Player)
                {
                    IssueGuard(entity.GroundPosition, entity);
                }
                else
                {
                    IssueGuard(point, null);
                }

                return;
            }

            if (entity != null && entity.Team == RtsTeam.Enemy)
            {
                IssueAttack(entity);
                return;
            }

            ResourceNode resource = hit.collider != null ? hit.collider.GetComponentInParent<ResourceNode>() : null;
            if (resource != null)
            {
                IssueHarvest(resource);
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

            PlayOrderVoice(RtsVoiceOrder.Attack);
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
            else
            {
                PlayOrderVoice(RtsVoiceOrder.Harvest);
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

            PlayOrderVoice(RtsVoiceOrder.Move);
        }

        private void IssueGuard(Vector3 point, RtsEntity target)
        {
            GatherSelectedUnits();
            int count = commandUnits.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 anchor = target != null ? target.GroundPosition + FormationOffset(i, count) : point + FormationOffset(i, count);
                commandUnits[i].IssueGuard(anchor, target);
            }

            if (count > 0)
            {
                Vector3 labelPoint = target != null ? target.GroundPosition : point;
                game.SpawnFloatingText(target != null ? "Guarding" : "Guard area", labelPoint + Vector3.up * 2f, new Color(0.75f, 1f, 0.45f));
                PlayOrderVoice(RtsVoiceOrder.Guard);
            }
        }

        private void PlayOrderVoice(RtsVoiceOrder order)
        {
            if (commandUnits.Count > 0 && game.Audio != null)
            {
                game.Audio.PlayOrder(order);
            }
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

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                game.TryQueueUnit(UnitKind.Rifleman);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                game.TryQueueUnit(UnitKind.RocketSoldier);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                game.TryQueueUnit(UnitKind.Grenadier);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                game.TryQueueUnit(UnitKind.FlameTrooper);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                game.TryQueueUnit(UnitKind.Engineer);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                game.TryQueueUnit(UnitKind.Harvester);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                game.TryQueueUnit(UnitKind.Tank);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                game.TryQueueUnit(UnitKind.Skyraider);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                game.TryQueueUnit(UnitKind.OrcaLifter);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                game.BuildManager.QueueStructure(StructureKind.PowerPlant);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                game.BuildManager.QueueStructure(StructureKind.Barracks);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                game.BuildManager.QueueStructure(StructureKind.Refinery);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                game.BuildManager.QueueStructure(StructureKind.WarFactory);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                game.BuildManager.QueueStructure(StructureKind.Turret);
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                game.BuildManager.QueueStructure(StructureKind.DualHelipad);
            }
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

        private void ReadButtons(out bool selectDown, out bool commandDown, out bool cancelDown, out bool guardHeld)
        {
            bool trigger = ReadXrButton(CommonUsages.triggerButton);
            bool grip = ReadXrButton(CommonUsages.gripButton);
            bool primary = ReadXrButton(CommonUsages.primaryButton);
            bool secondary = ReadXrButton(CommonUsages.secondaryButton);

            selectDown = Input.GetMouseButtonDown(0) || (trigger && !lastTrigger);
            commandDown = Input.GetMouseButtonDown(1) || (grip && !lastGrip) || (primary && !lastPrimary);
            cancelDown = Input.GetMouseButtonDown(2);
            guardHeld = secondary || Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            lastTrigger = trigger;
            lastGrip = grip;
            lastPrimary = primary;
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

        private void UpdateCommandPreview(Ray ray, bool guardHeld)
        {
            EnsureCommandPreview();

            if (IsPointerOverUi() || !HasCommandableSelection())
            {
                HideCommandPreview();
                return;
            }

            if (!TryGetCommandHit(ray, out RaycastHit hit, out Vector3 point))
            {
                HideCommandPreview();
                return;
            }

            RtsEntity entity = hit.collider != null ? hit.collider.GetComponentInParent<RtsEntity>() : null;
            Vector3 previewPoint = point;
            float radius = 1f;
            RtsCommandPreviewMode mode = RtsCommandPreviewMode.Move;

            if (guardHeld)
            {
                mode = RtsCommandPreviewMode.Guard;
                if (entity != null && entity.Team == RtsTeam.Player)
                {
                    previewPoint = entity.GroundPosition;
                    radius = Mathf.Max(1.2f, entity.SelectionRadius + 0.45f);
                }
            }
            else if (entity != null && entity.Team == RtsTeam.Enemy)
            {
                mode = RtsCommandPreviewMode.Target;
                previewPoint = entity.GroundPosition;
                radius = Mathf.Max(1.15f, entity.SelectionRadius + 0.45f);
            }

            commandPreview.SetPreview(mode, previewPoint, radius);
        }

        private void EnsureCommandPreview()
        {
            if (commandPreview != null)
            {
                return;
            }

            GameObject previewObject = new GameObject("Command Preview");
            commandPreview = previewObject.AddComponent<RtsCommandPreview>();
        }

        private void HideCommandPreview()
        {
            if (commandPreview != null)
            {
                commandPreview.Hide();
            }
        }

        private bool HasCommandableSelection()
        {
            for (int i = 0; i < game.Selection.Count; i++)
            {
                RtsUnit unit = game.Selection[i] as RtsUnit;
                if (unit != null && unit.Team == RtsTeam.Player)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetCommandHit(Ray ray, out RaycastHit hit, out Vector3 point)
        {
            if (Physics.Raycast(ray, out hit, 250f))
            {
                point = hit.point;
                point.y = 0f;
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                point.y = 0f;
                hit = default(RaycastHit);
                return true;
            }

            point = Vector3.zero;
            hit = default(RaycastHit);
            return false;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
