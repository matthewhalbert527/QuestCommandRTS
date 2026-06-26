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
        private bool lastTrigger;
        private bool lastGrip;
        private bool lastPrimary;
        private bool lastSecondary;

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

            HandleBuildHotkeys();

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
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
                CommandFromRay(ray);
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

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
