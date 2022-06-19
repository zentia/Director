using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class CinemaCurveTrackControl : TimelineTrackControl
	{
		protected int MINIMUM_ROWS_SHOWING = 3;
	
		protected int rowsShowing = 5;
	
		private CinemaCurveClipItemControl focusedControl;
	
		private int controlSelection;

        public override void UpdateTrackContents(DirectorControlState state, Rect position, Rect headerBackground)
        {
			if (state.IsInCurveMode)
            {
				base.UpdateTrackContents(state, position, headerBackground);
				return;
			}
			this.state = state;
			trackArea = position;
			var list = new List<KeyValuePair<int, TimelineItemWrapper>>();
			foreach (var current in itemMap.Keys)
            {
				var trackItemControl = itemMap[current];
				trackItemControl.Wrapper = current;
				trackItemControl.Track = TargetTrack;
				trackItemControl.PreUpdate(state, position);
				var item = new KeyValuePair<int, TimelineItemWrapper>(trackItemControl.DrawPriority, current);
				list.Add(item);
            }
			list.Sort(Comparison);
			foreach (var current in list)
            {
				itemMap[current.Value].HandleInput(state, position);
            }
			list.Reverse();
			var rect = new Rect(0f, 0f, position.width, position.height);
			var controlID = GUIUtility.GetControlID(TargetTrack.Behaviour.GetInstanceID(), FocusType.Passive, rect);
			headerBackground.height += 100;
			var cur = Event.current;
			var mp = cur.mousePosition;
			var mark = headerBackground.Contains(mp);
			var typeForControl = Event.current.GetTypeForControl(controlID);
			foreach (var current in list)
            {
				var value = itemMap[current.Value];
				value.Draw(state);
				value.PostUpdate(state, mark, typeForControl);
            }
			if (typeForControl == EventType.MouseDown && rect.Contains(mp) && cur.button == 1 && !cur.alt && !cur.shift && !cur.control)
            {
				showBodyContextMenu(cur);
				cur.Use();
            }
        }

        public override void UpdateHeaderContents(DirectorControlState state, Rect position, Rect headerBackground)
		{
			if (!state.IsInCurveMode)
			{
				base.UpdateHeaderContents(state, position, headerBackground);
				return;
			}
			Rect rect = new Rect(position.x + 14f, position.y, 14f, position.height);
			Rect rect2 = new Rect(rect.x + rect.width, position.y, position.width - 14f - 96f - 14f, 17f);
			string text = base.TargetTrack.Behaviour.name;
			bool flag = EditorGUI.Foldout(rect, isExpanded, GUIContent.none, false);
			if (flag != this.isExpanded)
			{
				this.isExpanded = flag;
				EditorPrefs.SetBool(base.IsExpandedKey, this.isExpanded);
			}
			updateHeaderControl1(new Rect(position.width - 64f, position.y, 16f, 16f));
			this.updateHeaderControl2(new Rect(position.width - 48f, position.y, 16f, 16f));
			this.updateHeaderControl3(new Rect(position.width - 32f, position.y, 16f, 16f));
			this.updateHeaderControl4(new Rect(position.width - 16f, position.y, 16f, 16f));
			if (isExpanded)
			{
				Rect row = new Rect(rect2.x + 28f, position.y + 17f, headerBackground.width - rect2.x - 28f, 17f);
				if (focusedControl != null && focusedControl.Wrapper != null && focusedControl.Wrapper.TimelineItem != null)
				{
					new Rect(row.x, row.y, row.width / 2f, row.height);
					Rect arg_26D_0 = new Rect(row.x + row.width / 2f, row.y, row.width / 2f, row.height);
					Rect controlHeaderArea = new Rect(row.x, row.y + row.height, row.width, position.height - 34f);
					focusedControl.UpdateHeaderArea(state, controlHeaderArea);
					if (GUI.Button(arg_26D_0, new GUIContent("Done")))
					{
						focusedControl = null;
						editItemControl(-1);
						MINIMUM_ROWS_SHOWING = 3;
						rowsShowing = 5;
					}
				}
				else
				{
					updateEditPanel(row);
				}
			}
			if (isRenaming)
			{
				GUI.SetNextControlName("CurveTrackRename");
				text = EditorGUI.TextField(rect2, GUIContent.none, text);
				if (renameRequested)
				{
					EditorGUI.FocusTextInControl("CurveTrackRename");
					renameRequested = false;
				}
				if ((int)Event.current.keyCode == 13 || (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
				{
					isRenaming = false;
					GUIUtility.hotControl=(0);
					GUIUtility.keyboardControl=(0);
				}
			}
			if (base.TargetTrack.Behaviour.name != text)
			{
				Undo.RecordObject(base.TargetTrack.Behaviour, string.Format("Renamed {0}", base.TargetTrack.Behaviour.name));
				base.TargetTrack.Behaviour.name=(text);
			}
			if (!this.isRenaming)
			{
				string text2 = text;
				Vector2 vector = GUI.skin.label.CalcSize(new GUIContent(text2));
				while (vector.x > rect2.width && text2.Length > 5)
				{
					text2 = text2.Substring(0, text2.Length - 4) + "...";
					vector = GUI.skin.label.CalcSize(new GUIContent(text2));
				}
				if (DirectorWindow.GetSelection().Contains(base.TargetTrack.Behaviour))
				{
					GUI.Label(rect2, text2, EditorStyles.whiteLabel);
				}
				else
				{
					GUI.Label(rect2, text2);
				}
				int controlID = GUIUtility.GetControlID(base.TargetTrack.Behaviour.GetInstanceID(), (FocusType)2, position);
				if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
				{
					if (position.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
					{
						if (!IsSelected)
						{
							base.RequestSelect();
						}
						base.showHeaderContextMenu();
						Event.current.Use();
						return;
					}
					if (position.Contains(Event.current.mousePosition) && (int)Event.current.button == 0)
					{
						base.RequestSelect();
						Event.current.Use();
					}
				}
			}
		}
	
		private void updateEditPanel(Rect row1)
		{
			Rect rect = new Rect(row1.x, row1.y, row1.width / 2f, row1.height);
			Rect rect2 = new Rect(row1.x + row1.width / 2f, row1.y, row1.width / 2f, row1.height);
			List<GUIContent> list = new List<GUIContent>();
			foreach (TrackItemControl current in base.Controls)
			{
				if (current.Wrapper != null && current.Wrapper.TimelineItem != null)
				{
					list.Add(new GUIContent(current.Wrapper.TimelineItem.name));
				}
			}
			int num = 1;
			for (int i = 0; i < list.Count - 1; i++)
			{
				int num2 = i + 1;
				while (num2 < list.Count && string.Compare(list[i].text, list[num2].text) == 0)
				{
					list[num2].text=(string.Format("{0} (duplicate {1})", list[num2].text, num++));
					num2++;
				}
				num = 1;
			}
			if (list.Count > 0)
			{
				controlSelection = Mathf.Min(controlSelection, list.Count - 1);
				controlSelection = EditorGUI.Popup(rect, controlSelection, list.ToArray());
				if (GUI.Button(rect2, new GUIContent("Edit")))
				{
					editItemControl(controlSelection);
				}
			}
		}
	
		private void editItemControl(int controlSelection)
		{
			int num = 0;
			using (IEnumerator<TrackItemControl> enumerator = Controls.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CinemaCurveClipItemControl cinemaCurveClipItemControl = (CinemaCurveClipItemControl)enumerator.Current;
					cinemaCurveClipItemControl.IsEditing = (num++ == controlSelection);
					if (cinemaCurveClipItemControl.IsEditing)
					{
						focusedControl = cinemaCurveClipItemControl;
						MINIMUM_ROWS_SHOWING = 10;
						rowsShowing = 10;
					}
				}
			}
		}
	
		private void togglEditItemControl(CinemaCurveClipItemControl control)
		{
			if (!control.IsEditing)
			{
				using (IEnumerator<TrackItemControl> enumerator = base.Controls.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						((CinemaCurveClipItemControl)enumerator.Current).IsEditing = false;
					}
				}
				control.IsEditing = true;
				this.focusedControl = control;
				this.MINIMUM_ROWS_SHOWING = 10;
				this.rowsShowing = 10;
				return;
			}
			using (IEnumerator<TrackItemControl> enumerator = base.Controls.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					((CinemaCurveClipItemControl)enumerator.Current).IsEditing = false;
				}
			}
			this.focusedControl = null;
			this.editItemControl(-1);
			this.MINIMUM_ROWS_SHOWING = 3;
			this.rowsShowing = 5;
		}
	
		protected override void updateHeaderControl2(Rect position)
		{
			if (!state.IsInCurveMode)
			{
				base.updateHeaderControl2(position);
				return;
			}
			if (GUI.Button(position, string.Empty, styles.compressStyle) && rowsShowing > MINIMUM_ROWS_SHOWING)
			{
				this.rowsShowing--;
			}
		}
	
		protected override void updateHeaderControl3(Rect position)
		{
			if (!state.IsInCurveMode)
			{
				base.updateHeaderControl3(position);
				return;
			}
			if (GUI.Button(position, string.Empty, styles.expandStyle))
			{
				this.rowsShowing++;
			}
		}
	
		protected override void initializeTrackItemControl(TrackItemControl control)
		{
			base.initializeTrackItemControl(control);
			if (control is CinemaCurveClipItemControl)
			{
				(control as CinemaCurveClipItemControl).RequestEdit += (this.curveClipControl_RequestEdit);
			}
		}
	
		protected override void prepareTrackItemControlForRemoval(TrackItemControl control)
		{
			base.prepareTrackItemControlForRemoval(control);
			if (control is CinemaCurveClipItemControl)
			{
				(control as CinemaCurveClipItemControl).RequestEdit -= new CurveClipWrapperEventHandler(this.curveClipControl_RequestEdit);
			}
		}
	
		private void curveClipControl_RequestEdit(object sender, CurveClipWrapperEventArgs e)
		{
			if (sender is CinemaCurveClipItemControl)
			{
				this.togglEditItemControl(sender as CinemaCurveClipItemControl);
			}
		}
	
		public override void CalculateHeight()
		{
			if (!TrackGroupControl.DirectorControl.InCurveMode)
			{
				base.CalculateHeight();
				return;
			}
			if (isExpanded)
			{
				trackArea.height = ROW_HEIGHT * (float)rowsShowing;
				return;
			}
			trackArea.height = ROW_HEIGHT;
		}

		private void Draw()
        {
			if (!isExpanded)
				return;
        }

		private void DrawCubic()
        {
			var track = targetTrack.Behaviour as TimelineTrack;
			for (var i = 0; i < track.Children.Count; i += 3)
            {
				var item0 = track.Children[i] as TimelineItem;
				var item1 = track.Children[i + 1] as TimelineItem;
				var idx = i + 2;
				if (idx > track.Children.Count)
                {
					idx = i + 1;
                }
				var item2 = track.Children[idx] as TimelineItem;
				idx = i + 3;
				if (idx > track.Children.Count)
                {
					idx = i + 1;
                }
				var item3 = track.Children[idx] as TimelineItem;
				Handles.DrawBezier(item0.position, item3.position, item1.position, item2.position, Color.white, null, 2f);
            }
        }

		private void DrawLine(Rect view)
        {
			var h = view.height;
			var y = view.y;
			var track = targetTrack.Behaviour as TimelineTrack;
			for (var i = 0; i < track.Children.Count; i++)
            {
				var item0 = track.Children[i] as TimelineItem;
				var item1 = track.Children[i + 1] as TimelineItem;
				Handles.DrawAAPolyLine(1.5f, item0.position, item1.position);
            }
        }
	}
}