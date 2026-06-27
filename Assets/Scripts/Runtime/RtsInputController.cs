using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuestCommandRTS
{
    public sealed class RtsInputController : MonoBehaviour
    {
        private readonly List<RtsEntity>[] controlGroups = new List<RtsEntity>[5];
        private RtsGame game;
        private RtsCommandDispatcher dispatcher;
        private bool mouseSelectionActive;
        private bool mouseDragging;
        private Vector2 mouseSelectionStart;
        private Rect mouseSelectionRect;
        private bool rightMouseTracking;
        private bool rightMouseDraggingCamera;
        private bool suppressRightClickCommand;
        private Vector2 rightMouseStart;
        private Vector2 rightMouseLast;
        private const float DragThreshold = 8f;
        private const float PointerRayDistance = 250f;
        private const float RightDragPanSpeed = 0.16f;

        public RtsCommandDispatcher SharedDispatcher => dispatcher;

        public void Initialize(RtsGame owner, RtsCommandDispatcher commandDispatcher)
        {
            game = owner;
            dispatcher = commandDispatcher;
            for (int i = 0; i < controlGroups.Length; i++)
            {
                controlGroups[i] = new List<RtsEntity>();
            }
        }

        private void Update()
        {
            if (game == null || dispatcher == null || game.CommandCamera == null)
            {
                return;
            }

            if (!game.AcceptsSystemInput)
            {
                mouseSelectionActive = false;
                mouseDragging = false;
                rightMouseTracking = false;
                rightMouseDraggingCamera = false;
                return;
            }

            if (HandleSystemHotkeys())
            {
                return;
            }

            if (!game.AcceptsPlayerInput)
            {
                mouseSelectionActive = false;
                mouseDragging = false;
                rightMouseTracking = false;
                rightMouseDraggingCamera = false;
                return;
            }

            HandleCameraMovement();
            HandleControlGroups();

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

            if (game.IsMatchOver)
            {
                return;
            }

            HandleBuildHotkeys();

            if (game.BuildManager != null && game.BuildManager.IsPlacing)
            {
                dispatcher.UpdatePlacement(ray);

                if (selectDown && !IsPointerOverUi())
                {
                    dispatcher.ConfirmPlacement();
                }

                if (commandDown || cancelDown)
                {
                    dispatcher.CancelPlacement();
                }

                return;
            }

            if (HandleTacticalHotkeys(ray))
            {
                return;
            }

            if (cancelDown)
            {
                dispatcher.ClearSelection();
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
                CommandFromRay(ray, guardHeld);
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
            dispatcher.SelectFromRay(ray, additive, PointerRayDistance);
        }

        private void CommandFromRay(Ray ray, bool guardHeld)
        {
            if (guardHeld)
            {
                dispatcher.GuardFromRay(ray, PointerRayDistance);
                return;
            }

            dispatcher.CommandFromRay(ray, PointerRayDistance);
        }

        private bool HandleTacticalHotkeys(Ray ray)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                return dispatcher.StopSelectedUnits() == RtsCommandResult.StopIssued;
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                return dispatcher.AttackMoveFromRay(ray, PointerRayDistance) == RtsCommandResult.AttackMoveIssued;
            }

            return false;
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
                game.PlayerCommands.QueueProduction(UnitKind.Rifleman);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                game.PlayerCommands.QueueProduction(UnitKind.Harvester);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                game.PlayerCommands.QueueProduction(UnitKind.Humvee);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                game.PlayerCommands.QueueProduction(UnitKind.Apc);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                game.PlayerCommands.QueueProduction(UnitKind.LightTank);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                game.PlayerCommands.QueueProduction(UnitKind.MediumTank);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                game.PlayerCommands.QueueProduction(UnitKind.HeavyTank);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                game.PlayerCommands.QueueProduction(UnitKind.Skyraider);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                game.PlayerCommands.QueueProduction(UnitKind.OrcaLifter);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.PowerPlant);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.Barracks);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.Refinery);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.WarFactory);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.Turret);
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                game.PlayerCommands.RequestConstruction(StructureKind.DualHelipad);
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                game.PlayerCommands.RepairSelectedStructures();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                game.PlayerCommands.SellSelectedStructures();
            }
        }

        private bool HandleSystemHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                game.ToggleUserPause();
                mouseSelectionActive = false;
                mouseDragging = false;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                game.ToggleUserPause();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                game.TryManualSave();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                game.TryManualLoad();
                return true;
            }

            return false;
        }

        private void HandleControlGroups()
        {
            for (int i = 0; i < controlGroups.Length; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha6 + i);
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
            Vector3 position = cameraTransform.position;

            if (Input.GetMouseButtonDown(1) && !IsPointerOverUi())
            {
                rightMouseTracking = true;
                rightMouseDraggingCamera = false;
                suppressRightClickCommand = false;
                rightMouseStart = Input.mousePosition;
                rightMouseLast = rightMouseStart;
            }

            if (rightMouseTracking && Input.GetMouseButton(1))
            {
                Vector2 current = Input.mousePosition;
                Vector2 totalDelta = current - rightMouseStart;
                Vector2 frameDelta = current - rightMouseLast;
                rightMouseLast = current;

                if (!rightMouseDraggingCamera && totalDelta.magnitude > DragThreshold)
                {
                    rightMouseDraggingCamera = true;
                }

                if (rightMouseDraggingCamera)
                {
                    Vector3 right = cameraTransform.right;
                    right.y = 0f;
                    right.Normalize();

                    Vector3 forward = cameraTransform.forward;
                    forward.y = 0f;
                    forward.Normalize();

                    position -= (right * frameDelta.x + forward * frameDelta.y) * RightDragPanSpeed;
                }
            }

            if (rightMouseTracking && Input.GetMouseButtonUp(1))
            {
                suppressRightClickCommand = rightMouseDraggingCamera;
                rightMouseTracking = false;
                rightMouseDraggingCamera = false;
            }

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

            if (move.sqrMagnitude > 0.01f)
            {
                float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 54f : 32f;
                position += move.normalized * speed * Time.deltaTime;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                position += cameraTransform.forward * scroll * 5.6f;
            }

            position.y = Mathf.Clamp(position.y, 18f, 92f);
            float cameraLimit = RtsBalance.MapHalfSize - 8f;
            position.x = Mathf.Clamp(position.x, -cameraLimit, cameraLimit);
            position.z = Mathf.Clamp(position.z, -cameraLimit - 18f, cameraLimit);
            cameraTransform.position = position;
        }

        private Ray GetPointerRay()
        {
            return game.CommandCamera.ScreenPointToRay(Input.mousePosition);
        }

        private void ReadButtons(out bool selectDown, out bool commandDown, out bool cancelDown, out bool guardHeld)
        {
            selectDown = Input.GetMouseButtonDown(0);
            commandDown = Input.GetMouseButtonUp(1) && !suppressRightClickCommand;
            if (Input.GetMouseButtonUp(1))
            {
                suppressRightClickCommand = false;
            }

            cancelDown = Input.GetMouseButtonDown(2);
            guardHeld = Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
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
