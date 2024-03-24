using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts.UI
{
	/// <summary>
	/// This script should be attached to each camera that's used to draw the objects with
	/// UI components on them. This may mean only one camera (main camera or your UI camera),
	/// or multiple cameras if you happen to have multiple viewports. Failing to attach this
	/// script simply means that objects drawn by this camera won't receive UI notifications:
	/// 
	/// - OnHover (isOver) is sent when the mouse hovers over a collider or moves away.
	/// - OnPress (isDown) is sent when a mouse button gets pressed on the collider.
	/// - OnSelect (selected) is sent when a mouse button is released on the same object as it was pressed on.
	/// - OnClick (int button) is sent with the same conditions as OnSelect, with the added check to see if the mouse has not moved much.
	/// - OnDoubleClick (int button) is sent when the click happens twice within a fourth of a second.
	/// - OnDrag (delta) is sent when a mouse or touch gets pressed on a collider and starts dragging it.
	/// - OnDrop (gameObject) is sent when the mouse or touch get released on a different collider than the one that was being dragged.
	/// - OnInput (text) is sent when typing (after selecting a collider by clicking on it).
	/// - OnTooltip (show) is sent when the mouse hovers over a collider for some time without moving.
	/// - OnScroll (float delta) is sent out when the mouse scroll wheel is moved.
	/// - OnKey (KeyCode key) is sent when keyboard or controller input is used.
	/// </summary>

	public class UICamera : MonoBehaviour
	{
		/// <summary>
		/// Whether the touch event will be sending out the OnClick notification at the end.
		/// </summary>
		private enum ClickNotification
		{
			None,
			Always,
			BasedOnDelta,
		}

		/// <summary>
		/// Ambiguous mouse, touch, or controller event.
		/// </summary>
		private class MouseOrTouch
		{
			public Vector2 pos;				// Current position of the mouse or touch event
			public Vector2 delta;			// Delta since last update
			public Vector2 totalDelta;		// Delta since the event started being tracked

			public Camera pressedCam;		// Camera that the OnPress(true) was fired with

			public GameObject current;		// The current game object under the touch or mouse
			public GameObject pressed;		// The last game object to receive OnPress

			public float clickTime;	// The last time a click event was sent out

			public ClickNotification clickNotification = ClickNotification.Always;
		}

		class Highlighted
		{
			public GameObject go;
			public int counter;
		}

		/// <summary>
		/// Whether the mouse input is used.
		/// </summary>

		public bool useMouse = true;

		/// <summary>
		/// Whether the touch-based input is used.
		/// </summary>

		public bool useTouch = true;

		/// <summary>
		/// Whether the keyboard events will be processed.
		/// </summary>

		public bool useKeyboard = true;

		/// <summary>
		/// Whether the joystick and controller events will be processed.
		/// </summary>

		public bool useController = true;

		/// <summary>
		/// Which layers will receive events.
		/// </summary>

		public LayerMask eventReceiverMask = -1;

		/// <summary>
		/// How long of a delay to expect before showing the tooltip.
		/// </summary>

		public float tooltipDelay = 1f;

		/// <summary>
		/// Whether the tooltip will disappear as soon as the mouse moves (false) or only if the mouse moves outside of the widget's area (true).
		/// </summary>

		public bool stickyTooltip = true;

		/// <summary>
		/// How far the mouse is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
		/// </summary>

		public float mouseClickThreshold = 10f;

		/// <summary>
		/// How far the touch is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
		/// </summary>

		public float touchClickThreshold = 40f;

		/// <summary>
		/// Raycast range distance. By default it's as far as the camera can see.
		/// </summary>

		public float rangeDistance = -1f;

		/// <summary>
		/// Name of the axis used for scrolling.
		/// </summary>

		public string scrollAxisName = "Mouse ScrollWheel";

		/// <summary>
		/// Name of the axis used to send up and down key events.
		/// </summary>

		public string verticalAxisName = "Left_Stick_Vert";

		/// <summary>
		/// Name of the axis used to send left and right key events.
		/// </summary>

		public string horizontalAxisName = "Left_Stick_Hors";

		/// <summary>
		/// Various keys used by the camera.
		/// </summary>

		public KeyCode submitKey0 = KeyCode.Return;
		public KeyCode submitKey1 = KeyCode.JoystickButton0;
		public KeyCode cancelKey0 = KeyCode.Escape;
		public KeyCode cancelKey1 = KeyCode.JoystickButton1;

		[Obsolete("Use UICamera.currentCamera instead")]
		public static Camera LastCamera => _currentCamera;

		[Obsolete("Use UICamera.currentTouchID instead")]
		public static int LastTouchID => _currentTouchID;

		/// <summary>
		/// Position of the last touch (or mouse) event.
		/// </summary>
		private static Vector2 _lastTouchPosition = Vector2.zero;

		/// <summary>
		/// Last raycast hit prior to sending out the event. This is useful if you want detailed information
		/// about what was actually hit in your OnClick, OnHover, and other event functions.
		/// </summary>
		private static RaycastHit _lastHit;

		/// <summary>
		/// Last camera active prior to sending out the event. This will always be the camera that actually sent out the event.
		/// </summary>
		private static Camera _currentCamera;

		/// <summary>
		/// ID of the touch or mouse operation prior to sending out the event. Mouse ID is '-1' for left, '-2' for right mouse button, '-3' for middle.
		/// </summary>
		private static int _currentTouchID = -1;

		/// <summary>
		/// Current touch, set before any event function gets called.
		/// </summary>
		private static MouseOrTouch _currentTouch;

		/// <summary>
		/// Whether an input field currently has focus.
		/// </summary>
		private static bool _inputHasFocus;

		/// <summary>
		/// If events don't get handled, they will be forwarded to this game object.
		/// </summary>

		public static GameObject fallThrough;

		// List of all active cameras in the scene
		private static readonly List<UICamera> MList = new List<UICamera>();

		// List of currently highlighted items
		private static readonly List<Highlighted> MHighlighted = new List<Highlighted>();

		// Selected widget (for input)
		private static GameObject _mSel;

		// Mouse events
		private static readonly MouseOrTouch[] MMouse = new MouseOrTouch[] { new MouseOrTouch(), new MouseOrTouch(), new MouseOrTouch() };

		// The last object to receive OnHover

		// Joystick/controller/keyboard event
		private static readonly MouseOrTouch MController = new MouseOrTouch();

		// Used to ensure that joystick-based controls don't trigger that often
		private static float _mNextEvent;

		// List of currently active touches
		private readonly Dictionary<int, MouseOrTouch> _mTouches = new Dictionary<int, MouseOrTouch>();

		// Tooltip widget (mouse only)
		private GameObject _mTooltip;

		// Mouse input is turned off on iOS
		private Camera _mCam;
		private LayerMask _mLayerMask;
		private float _mTooltipTime;
		private bool _mIsEditor;

		/// <summary>
		/// Helper function that determines if this script should be handling the events.
		/// </summary>

		private bool HandlesEvents => EventHandler == this;

		/// <summary>
		/// Caching is always preferable for performance.
		/// </summary>

		private Camera CachedCamera { get { if (_mCam == null) _mCam = GetComponent<Camera>(); return _mCam; } }

		/// <summary>
		/// The object the mouse is hovering over.
		/// </summary>

		private static GameObject HoveredObject { get; set; }

		/// <summary>
		/// Option to manually set the selected game object.
		/// </summary>

		public static GameObject SelectedObject
		{
			get => _mSel;
			set
			{
				if (_mSel != value)
				{
					if (_mSel != null)
					{
						UICamera uiCam = FindCameraForLayer(_mSel.layer);
					
						if (uiCam != null)
						{
							_currentCamera = uiCam._mCam;
							_mSel.SendMessage("OnSelect", false, SendMessageOptions.DontRequireReceiver);
							if (uiCam.useController || uiCam.useKeyboard) Highlight(_mSel, false);
						}
					}

					_mSel = value;

					if (_mSel != null)
					{
						UICamera uiCam = FindCameraForLayer(_mSel.layer);

						if (uiCam != null)
						{
							_currentCamera = uiCam._mCam;
							if (uiCam.useController || uiCam.useKeyboard) Highlight(_mSel, true);
							_mSel.SendMessage("OnSelect", true, SendMessageOptions.DontRequireReceiver);
						}
					}
				}
			}
		}

		/// <summary>
		/// Clear the list on application quit (also when Play mode is exited)
		/// </summary>

		void OnApplicationQuit () { MHighlighted.Clear(); }

		/// <summary>
		/// Convenience function that returns the main HUD camera.
		/// </summary>

		public static Camera MainCamera
		{
			get
			{
				UICamera mouse = EventHandler;
				return (mouse != null) ? mouse.CachedCamera : null;
			}
		}

		/// <summary>
		/// Event handler for all types of events.
		/// </summary>

		private static UICamera EventHandler
		{
			get
			{
				return MList.FirstOrDefault(cam => cam != null && cam.enabled && cam.gameObject.activeInHierarchy);
			}
		}

		/// <summary>
		/// Static comparison function used for sorting.
		/// </summary>

		static int CompareFunc (UICamera a, UICamera b)
		{
			if (a.CachedCamera.depth < b.CachedCamera.depth) return 1;
			if (a.CachedCamera.depth > b.CachedCamera.depth) return -1;
			return 0;
		}

		/// <summary>
		/// Returns the object under the specified position.
		/// </summary>

		static bool Raycast (Vector3 inPos, ref RaycastHit hit)
		{
			foreach (var cam in MList.Where(cam => cam.enabled && cam.gameObject.activeInHierarchy))
			{
				// Convert to view space
				_currentCamera = cam.CachedCamera;
				Vector3 pos = _currentCamera.ScreenToViewportPoint(inPos);

				// If it's outside the camera's viewport, do nothing
				if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f) continue;

				// Cast a ray into the screen
				var ray = _currentCamera.ScreenPointToRay(inPos);

				// Raycast into the screen
				var mask = _currentCamera.cullingMask & cam.eventReceiverMask;
				var dist = (cam.rangeDistance > 0f) ? cam.rangeDistance : _currentCamera.farClipPlane - _currentCamera.nearClipPlane;
				if (Physics.Raycast(ray, out hit, dist, mask)) return true;
			}

			return false;
		}

		/// <summary>
		/// Find the camera responsible for handling events on objects of the specified layer.
		/// </summary>
		private static UICamera FindCameraForLayer (int layer)
		{
			int layerMask = 1 << layer;

			foreach (var cam in MList)
			{
				Camera uc = cam.CachedCamera;
				if ((uc != null) && (uc.cullingMask & layerMask) != 0) return cam;
			}
			return null;
		}

		/// <summary>
		/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
		/// </summary>

		static int GetDirection (KeyCode up, KeyCode down)
		{
			if (Input.GetKeyDown(up)) return 1;
			if (Input.GetKeyDown(down)) return -1;
			return 0;
		}

		/// <summary>
		/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
		/// </summary>

		static int GetDirection (KeyCode up0, KeyCode up1, KeyCode down0, KeyCode down1)
		{
			if (Input.GetKeyDown(up0) || Input.GetKeyDown(up1)) return 1;
			if (Input.GetKeyDown(down0) || Input.GetKeyDown(down1)) return -1;
			return 0;
		}

		/// <summary>
		/// Using the joystick to move the UI results in 1 or -1 if the threshold has been passed, mimicking up/down keys.
		/// </summary>

		static int GetDirection (string axis)
		{
			float time = Time.realtimeSinceStartup;

			if (_mNextEvent < time)
			{
				float val = Input.GetAxis(axis);

				if (val > 0.75f)
				{
					_mNextEvent = time + 0.25f;
					return 1;
				}

				if (val < -0.75f)
				{
					_mNextEvent = time + 0.25f;
					return -1;
				}
			}
			return 0;
		}

		/// <summary>
		/// Returns whether the widget should be currently highlighted as far as the UICamera knows.
		/// </summary>

		public static bool IsHighlighted (GameObject go)
		{
			for (int i = MHighlighted.Count; i > 0; )
			{
				Highlighted hl = MHighlighted[--i];
				if (hl.go == go) return true;
			}
			return false;
		}

		/// <summary>
		/// Apply or remove highlighted (hovered) state from the specified object.
		/// </summary>

		static void Highlight (GameObject go, bool highlighted)
		{
			if (go != null)
			{
				for (int i = MHighlighted.Count; i > 0; )
				{
					Highlighted hl = MHighlighted[--i];

					if (hl == null || hl.go == null)
					{
						MHighlighted.RemoveAt(i);
					}
					else if (hl.go == go)
					{
						if (highlighted)
						{
							++hl.counter;
						}
						else if (--hl.counter < 1)
						{
							MHighlighted.Remove(hl);
							go.SendMessage("OnHover", false, SendMessageOptions.DontRequireReceiver);
						}
						return;
					}
				}

				if (highlighted)
				{
					var hl = new Highlighted
					{
						go = go,
						counter = 1
					};
					MHighlighted.Add(hl);
					go.SendMessage("OnHover", true, SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		/// <summary>
		/// Get or create a touch event.
		/// </summary>

		MouseOrTouch GetTouch (int id)
		{
			if (_mTouches.TryGetValue(id, out var touch)) return touch;
			touch = new MouseOrTouch();
			_mTouches.Add(id, touch);
			return touch;
		}

		/// <summary>
		/// Remove a touch event from the list.
		/// </summary>

		void RemoveTouch (int id)
		{
			_mTouches.Remove(id);
		}

		/// <summary>
		/// Add this camera to the list.
		/// </summary>

		[Obsolete("Obsolete")]
		void Awake ()
		{
			if (Application.platform == RuntimePlatform.Android ||
			    Application.platform == RuntimePlatform.IPhonePlayer)
			{
				useMouse = false;
				useTouch = true;
				useKeyboard = false;
				useController = false;
			}
			else if (Application.platform == RuntimePlatform.PS3 ||
			         Application.platform == RuntimePlatform.XBOX360)
			{
				useMouse = false;
				useTouch = false;
				useKeyboard = false;
				useController = true;
			}
			else if (Application.platform == RuntimePlatform.WindowsEditor ||
			         Application.platform == RuntimePlatform.OSXEditor)
			{
				_mIsEditor = true;
			}

			// Save the starting mouse position
			MMouse[0].pos.x = Input.mousePosition.x;
			MMouse[0].pos.y = Input.mousePosition.y;
			_lastTouchPosition = MMouse[0].pos;

			// Add this camera to the list
			MList.Add(this);
			MList.Sort(CompareFunc);

			// If no event receiver mask was specified, use the camera's mask
			if (eventReceiverMask == -1) eventReceiverMask = GetComponent<Camera>().cullingMask;
		}

		/// <summary>
		/// Remove this camera from the list.
		/// </summary>
		private void OnDestroy ()
		{
			MList.Remove(this);
		}

		/// <summary>
		/// Update the object under the mouse if we're not using touch-based input.
		/// </summary>
		private void FixedUpdate ()
		{
			if (!useMouse || !Application.isPlaying || !HandlesEvents) return;
			var go = Raycast(Input.mousePosition, ref _lastHit) ? _lastHit.collider.gameObject : fallThrough;
			for (var i = 0; i < 3; ++i) MMouse[i].current = go;
		}

		/// <summary>
		/// Check the input and send out appropriate events.
		/// </summary>
		private void Update ()
		{
			// Only the first UI layer should be processing events
			if (!Application.isPlaying || !HandlesEvents) return;

			// Update mouse input
			if (useMouse || (useTouch && _mIsEditor)) ProcessMouse();

			// Process touch input
			if (useTouch) ProcessTouches();

			// Clear the selection on the cancel key, but only if mouse input is allowed
			if (useMouse && _mSel != null && (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0)) ||
			    (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1))) SelectedObject = null;

			// Forward the input to the selected object
			if (_mSel != null)
			{
				string input = Input.inputString;

				// Adding support for some macs only having the "Delete" key instead of "Backspace"
				if (useKeyboard && Input.GetKeyDown(KeyCode.Delete)) input += "\b";

				if (input.Length > 0)
				{
					if (!stickyTooltip && _mTooltip != null) ShowTooltip(false);
					_mSel.SendMessage("OnInput", input, SendMessageOptions.DontRequireReceiver);
				}

				// Update the keyboard and joystick events
				ProcessOthers();
			}
			else _inputHasFocus = false;

			// If it's time to show a tooltip, inform the object we're hovering over
			if (useMouse && HoveredObject != null)
			{
				float scroll = Input.GetAxis(scrollAxisName);
				if (scroll != 0f) HoveredObject.SendMessage("OnScroll", scroll, SendMessageOptions.DontRequireReceiver);

				if (_mTooltipTime != 0f && _mTooltipTime < Time.realtimeSinceStartup)
				{
					_mTooltip = HoveredObject;
					ShowTooltip(true);
				}
			}
		}

		/// <summary>
		/// Update mouse input.
		/// </summary>

		void ProcessMouse ()
		{
			bool updateRaycast = (Time.timeScale < 0.9f);

			if (!updateRaycast)
			{
				for (int i = 0; i < 3; ++i)
				{
					if (Input.GetMouseButton(i) || Input.GetMouseButtonUp(i))
					{
						updateRaycast = true;
						break;
					}
				}
			}

			// Update the position and delta
			MMouse[0].pos = Input.mousePosition;
			MMouse[0].delta = MMouse[0].pos - _lastTouchPosition;

			bool posChanged = (MMouse[0].pos != _lastTouchPosition);
			_lastTouchPosition = MMouse[0].pos;

			// Update the object under the mouse
			if (updateRaycast) MMouse[0].current = Raycast(Input.mousePosition, ref _lastHit) ? _lastHit.collider.gameObject : fallThrough;

			// Propagate the updates to the other mouse buttons
			for (int i = 1; i < 3; ++i)
			{
				MMouse[i].pos = MMouse[0].pos;
				MMouse[i].delta = MMouse[0].delta;
				MMouse[i].current = MMouse[0].current;
			}

			// Is any button currently pressed?
			bool isPressed = false;

			for (int i = 0; i < 3; ++i)
			{
				if (Input.GetMouseButton(i))
				{
					isPressed = true;
					break;
				}
			}

			if (isPressed)
			{
				// A button was pressed -- cancel the tooltip
				_mTooltipTime = 0f;
			}
			else if (posChanged && (!stickyTooltip || HoveredObject != MMouse[0].current))
			{
				if (_mTooltipTime != 0f)
				{
					// Delay the tooltip
					_mTooltipTime = Time.realtimeSinceStartup + tooltipDelay;
				}
				else if (_mTooltip != null)
				{
					// Hide the tooltip
					ShowTooltip(false);
				}
			}

			// The button was released over a different object -- remove the highlight from the previous
			if (!isPressed && HoveredObject != null && HoveredObject != MMouse[0].current)
			{
				if (_mTooltip != null) ShowTooltip(false);
				Highlight(HoveredObject, false);
				HoveredObject = null;
			}

			// Process all 3 mouse buttons as individual touches
			for (int i = 0; i < 3; ++i)
			{
				bool pressed = Input.GetMouseButtonDown(i);
				bool unpressed = Input.GetMouseButtonUp(i);

				_currentTouch = MMouse[i];
				_currentTouchID = -1 - i;

				// We don't want to update the last camera while there is a touch happening
				if (pressed) _currentTouch.pressedCam = _currentCamera;
				else if (_currentTouch.pressed != null) _currentCamera = _currentTouch.pressedCam;

				// Process the mouse events
				ProcessTouch(pressed, unpressed);
			}
			_currentTouch = null;

			// If nothing is pressed and there is an object under the touch, highlight it
			if (!isPressed && HoveredObject != MMouse[0].current)
			{
				_mTooltipTime = Time.realtimeSinceStartup + tooltipDelay;
				HoveredObject = MMouse[0].current;
				Highlight(HoveredObject, true);
			}
		}

		/// <summary>
		/// Update touch-based events.
		/// </summary>

		void ProcessTouches ()
		{
			for (int i = 0; i < Input.touchCount; ++i)
			{
				Touch input = Input.GetTouch(i);
				_currentTouchID = input.fingerId;
				_currentTouch = GetTouch(_currentTouchID);

				bool pressed = (input.phase == TouchPhase.Began);
				bool unpressed = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);

				if (pressed)
				{
					_currentTouch.delta = Vector2.zero;
				}
				else
				{
					// Although input.deltaPosition can be used, calculating it manually is safer (just in case)
					_currentTouch.delta = input.position - _currentTouch.pos;
				}

				_currentTouch.pos = input.position;
				_currentTouch.current = Raycast(_currentTouch.pos, ref _lastHit) ? _lastHit.collider.gameObject : fallThrough;
				_lastTouchPosition = _currentTouch.pos;

				// We don't want to update the last camera while there is a touch happening
				if (pressed) _currentTouch.pressedCam = _currentCamera;
				else if (_currentTouch.pressed != null) _currentCamera = _currentTouch.pressedCam;

				// Process the events from this touch
				ProcessTouch(pressed, unpressed);

				// If the touch has ended, remove it from the list
				if (unpressed) RemoveTouch(_currentTouchID);
				_currentTouch = null;
			}
		}

		/// <summary>
		/// Process keyboard and joystick events.
		/// </summary>

		void ProcessOthers ()
		{
			_currentTouchID = -100;
			_currentTouch = MController;

			// If this is an input field, ignore WASD and Space key presses
			_inputHasFocus = (_mSel != null && _mSel.GetComponent<UIInput>() != null);

			bool submitKeyDown = (submitKey0 != KeyCode.None && Input.GetKeyDown(submitKey0)) || (submitKey1 != KeyCode.None && Input.GetKeyDown(submitKey1));
			bool submitKeyUp = (submitKey0 != KeyCode.None && Input.GetKeyUp(submitKey0)) || (submitKey1 != KeyCode.None && Input.GetKeyUp(submitKey1));

			if (submitKeyDown || submitKeyUp)
			{
				_currentTouch.current = _mSel;
				ProcessTouch(submitKeyDown, submitKeyUp);
			}

			int vertical = 0;
			int horizontal = 0;

			if (useKeyboard)
			{
				if (_inputHasFocus)
				{
					vertical += GetDirection(KeyCode.UpArrow, KeyCode.DownArrow);
					horizontal += GetDirection(KeyCode.RightArrow, KeyCode.LeftArrow);
				}
				else
				{
					vertical += GetDirection(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow);
					horizontal += GetDirection(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
				}
			}

			if (useController)
			{
				if (!string.IsNullOrEmpty(verticalAxisName)) vertical += GetDirection(verticalAxisName);
				if (!string.IsNullOrEmpty(horizontalAxisName)) horizontal += GetDirection(horizontalAxisName);
			}

			// Send out key notifications
			if (vertical != 0) _mSel.SendMessage("OnKey", vertical > 0 ? KeyCode.UpArrow : KeyCode.DownArrow, SendMessageOptions.DontRequireReceiver);
			if (horizontal != 0) _mSel.SendMessage("OnKey", horizontal > 0 ? KeyCode.RightArrow : KeyCode.LeftArrow, SendMessageOptions.DontRequireReceiver);
			if (useKeyboard && Input.GetKeyDown(KeyCode.Tab)) _mSel.SendMessage("OnKey", KeyCode.Tab, SendMessageOptions.DontRequireReceiver);

			// Send out the cancel key notification
			if (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0)) _mSel.SendMessage("OnKey", KeyCode.Escape, SendMessageOptions.DontRequireReceiver);
			if (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1)) _mSel.SendMessage("OnKey", KeyCode.Escape, SendMessageOptions.DontRequireReceiver);

			_currentTouch = null;
		}

		/// <summary>
		/// Process the events of the specified touch.
		/// </summary>

		void ProcessTouch (bool pressed, bool unpressed)
		{
			// Send out the press message
			if (pressed)
			{
				if (_mTooltip != null) ShowTooltip(false);
				_currentTouch.pressed = _currentTouch.current;
				_currentTouch.clickNotification = ClickNotification.Always;
				_currentTouch.totalDelta = Vector2.zero;
				if (_currentTouch.pressed != null) _currentTouch.pressed.SendMessage("OnPress", true, SendMessageOptions.DontRequireReceiver);

				// Clear the selection
				if (_currentTouch.pressed != _mSel)
				{
					if (_mTooltip != null) ShowTooltip(false);
					SelectedObject = null;
				}
			}
			else if (_currentTouch.pressed != null && _currentTouch.delta.magnitude != 0f)
			{
				if (_mTooltip != null) ShowTooltip(false);
				_currentTouch.totalDelta += _currentTouch.delta;

				bool isDisabled = (_currentTouch.clickNotification == ClickNotification.None);
				_currentTouch.pressed.SendMessage("OnDrag", _currentTouch.delta, SendMessageOptions.DontRequireReceiver);

				if (isDisabled)
				{
					// If the notification status has already been disabled, keep it as such
					_currentTouch.clickNotification = ClickNotification.None;
				}
				else if (_currentTouch.clickNotification == ClickNotification.BasedOnDelta)
				{
					// If the notification is based on delta and the delta gets exceeded, disable the notification
					float threshold = (_currentTouch == MMouse[0]) ? mouseClickThreshold : Mathf.Max(touchClickThreshold, Screen.height * 0.1f);

					if (_currentTouch.totalDelta.magnitude > threshold)
					{
						_currentTouch.clickNotification = ClickNotification.None;
					}
				}
			}

			// Send out the undress message
			if (unpressed)
			{
				if (_mTooltip != null) ShowTooltip(false);

				if (_currentTouch.pressed != null)
				{
					_currentTouch.pressed.SendMessage("OnPress", false, SendMessageOptions.DontRequireReceiver);

					// Send a hover message to the object, but don't add it to the list of hovered items as it's already present
					// This happens when the mouse is released over the same button it was pressed on, and since it already had
					// its 'OnHover' event, it never got Highlight(false), so we simply re-notify it so it can update the visible state.
					if (useMouse && _currentTouch.pressed == HoveredObject) _currentTouch.pressed.SendMessage("OnHover", true, SendMessageOptions.DontRequireReceiver);

					// If the button/touch was released on the same object, consider it a click and select it
					if (_currentTouch.pressed == _currentTouch.current)
					{
						if (_currentTouch.pressed != _mSel)
						{
							_mSel = _currentTouch.pressed;
							_currentTouch.pressed.SendMessage("OnSelect", true, SendMessageOptions.DontRequireReceiver);
						}
						else
						{
							_mSel = _currentTouch.pressed;
						}

						// If the touch should consider clicks, send out an OnClick notification
						if (_currentTouch.clickNotification != ClickNotification.None)
						{
							float time = Time.realtimeSinceStartup;

							_currentTouch.pressed.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);

							if (_currentTouch.clickTime + 0.25f > time)
							{
								_currentTouch.pressed.SendMessage("OnDoubleClick", SendMessageOptions.DontRequireReceiver);
							}
							_currentTouch.clickTime = time;
						}
					}
					else // The button/touch was released on a different object
					{
						// Send a drop notification (for drag & drop)
						if (_currentTouch.current != null) _currentTouch.current.SendMessage("OnDrop", _currentTouch.pressed, SendMessageOptions.DontRequireReceiver);
					}
				}
				_currentTouch.pressed = null;
			}
		}

		/// <summary>
		/// Show or hide the tooltip.
		/// </summary>
		private void ShowTooltip (bool val)
		{
			_mTooltipTime = 0f;
			if (_mTooltip != null) _mTooltip.SendMessage("OnTooltip", val, SendMessageOptions.DontRequireReceiver);
			if (!val) _mTooltip = null;
		}
	}
}