using System;
using UnityEngine;
using System.Collections.Generic;

namespace CinemaDirector
{
	[Serializable]
	public abstract class DirectorBehaviourControl
	{
		private DirectorObject behaviour;

		public event DirectorBehaviourControlHandler DeleteRequest;
		public event DirectorBehaviourControlHandler DuplicateRequest;

		private HashSet<KeyCode> keyCodes = new HashSet<KeyCode>();
		private HashSet<KeyCode> globalKeyCodes = new HashSet<KeyCode>();
		private Dictionary<KeyCode, ControlCommond> keycodeCommand;
		private Dictionary<KeyCode, ControlCommond> globalKeycodeCommand;
		public Dictionary<string, ControlCommond> shortcutCommond;
		public Dictionary<string, ControlCommond> globalShortcutCommand;
		public DirectorObject Behaviour
		{
			get
			{
				return behaviour;
			}
			set
			{
				behaviour = value;
			}
		}

		public bool IsSelected
		{
			get
			{
				return Behaviour != null && DirectorWindow.GetSelection().Contains(Behaviour);
			}
		}

		public void RequestDelete()
		{
			if (DeleteRequest != null)
			{
				DeleteRequest(this, new DirectorBehaviourControlEventArgs(behaviour, this));
			}
		}

		public void RequestDuplicate()
		{
			if (DuplicateRequest != null)
			{
				DuplicateRequest(this, new DirectorBehaviourControlEventArgs(Behaviour, this));
			}
		}

		protected void InitCommand()
        {
			if (keycodeCommand == null)
            {
				keycodeCommand = new Dictionary<KeyCode, ControlCommond>();
				keycodeCommand[KeyCode.F2] = new ControlCommond
				{
					OnKeyUp = Rename,
				};
				keycodeCommand[KeyCode.Delete] = new ControlCommond 
				{
					OnKeyUp = RequestDelete,
				};
            }

			if (globalKeycodeCommand == null)
			{
				globalKeycodeCommand = new Dictionary<KeyCode, ControlCommond>();
				globalKeycodeCommand[KeyCode.K] = new ControlCommond
				{
					OnKeyUp = Record,
				};
			}
			if (shortcutCommond == null)
            {
				shortcutCommond = new Dictionary<string, ControlCommond>();
				shortcutCommond["Paste"] = new ControlCommond
				{
					OnValidate = () => { },
					OnExecute = Paste,
				};
				shortcutCommond["Duplicate"] = new ControlCommond
				{
					OnValidate = () => { },
					OnExecute = RequestDuplicate,
				};
            }

			if (globalShortcutCommand == null)
			{
				globalShortcutCommand = new Dictionary<string, ControlCommond>();
			}
        }

		public void OnGlobalEvent()
        {
			if (DirectorWindow.GetSelection().activeObject == behaviour)
            {
	            ExecuteGlobalCommand(Event.current);
            }
        }

		private void ExecuteGlobalCommand(Event e)
		{
			ExecuteCommand(globalShortcutCommand, globalKeycodeCommand, globalKeyCodes, e);
		}
		
		protected void ExecuteCommand(Event e)
        {
			ExecuteCommand(shortcutCommond, keycodeCommand, keyCodes, e);
        }

		private void ExecuteCommand(Dictionary<string, ControlCommond> shortcutCmd,
			Dictionary<KeyCode, ControlCommond> keycodeCmd, HashSet<KeyCode> kds, Event e)
		{
			ControlCommond command;
			switch (e.type)
			{
				case EventType.ValidateCommand:
					shortcutCmd.TryGetValue(e.commandName, out command);
					if (command != null && command.OnValidate != null)
					{
						command.OnValidate();
						e.Use();
					}
					break;
				case EventType.ExecuteCommand:
					shortcutCmd.TryGetValue(e.commandName, out command);
					if (command != null && command.OnExecute != null)
					{
						command.OnExecute();
						e.Use();
					}
					break;
				case EventType.KeyDown:
					kds.Add(e.keyCode);
					keycodeCmd.TryGetValue(e.keyCode, out command);
					if (command != null && command.OnKeyDown != null)
					{
						command.OnKeyDown(e.keyCode);
						e.Use();
					}
					break;
				case EventType.KeyUp:
					kds.Remove(e.keyCode);
					keycodeCmd.TryGetValue(e.keyCode, out command);
					if (command != null && command.OnKeyUp != null)
					{
						command.OnKeyUp();
						e.Use();
					}
					break;
			}
		}

		protected virtual void Paste()
        {

        }

		public virtual DirectorObject Duplicate(DirectorObject parent = null)
        {
			return null;
        }

		protected virtual void Rename()
        {

        }

		protected virtual void Record()
        {

        }

		internal void Delete()
        {
			Behaviour.Destroy(true);
        }
	}
}
