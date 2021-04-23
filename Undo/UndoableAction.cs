using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Undo
{
    /// <summary>
    /// Base class for actions that can be undone.
    /// </summary>
    /// <remarks>
    /// Actions can form trees. For example a graph, where we can add/remove nodes and edges. Removing a node removes the connected edges, but those
    /// are possible actions too, no reason not to use them. This means that the remove node action will contain "remove edge" children. Those in turn
    /// could contain other changes, e.g. renames, cascading node removals, etc.
    /// Another use of this feature is to create groups (batches) of actions, that should be undone/redone together.
    /// </remarks>
    public class UndoableAction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public UndoableAction()
        {
            time = DateTime.UtcNow;
        }

        /// <summary>
        /// A human-readable description of the change, e.g. to be displayed in the UI.
        /// </summary>
        public string Description
        {
            get => description;
            set
            {
                if (description == value)
                    return;
                description = value;
                RaisePropertyChanged();
            }
        }
        protected string description;

        public override string ToString() => Description;

        /// <summary>
        /// When did the action happen.
        /// </summary>
        public DateTime Time
        {
            get => time;
            set
            {
                if (time == value)
                    return;
                time = value;
                RaisePropertyChanged();
            }
        }
        protected DateTime time;

        /// <summary>
        /// Actions that were executed while this one was created.
        /// </summary>
        public ReadOnlyObservableCollection<UndoableAction> Children { get; private set; }
        ObservableCollection<UndoableAction> children;

        internal void CreateChildren()
        {
            if (children == null)
            {
                children = new ObservableCollection<UndoableAction>();
                Children = new ReadOnlyObservableCollection<UndoableAction>(children);
            }
        }

        internal void RemoveLastChild()
        {
            children.RemoveAt(children.Count - 1);
            LatestChildIndex = Math.Min(LatestChildIndex, children.Count - 1);
            if (!CanUndoChild)
                RaisePropertyChanged(nameof(CanUndoChild));
        }

        internal void AddChild(UndoableAction action)
        {
            children.Add(action);
            LatestChildIndex++;
            if (latestChildIndex == 0)
                RaisePropertyChanged(nameof(CanUndoChild));
        }

        /// <summary>
        /// Index of the latest performed child action. -1 if no prior actions.
        /// </summary>
        public int LatestChildIndex
        {
            get => latestChildIndex;
            private set
            {
                if (latestChildIndex == value)
                    return;

                latestChildIndex = value;
                RaisePropertyChanged();
            }
        }
        int latestChildIndex = -1;

        /// <summary>
        /// Is there a child action to undo?
        /// </summary>
        public bool CanUndoChild => children != null && latestChildIndex >= 0;

        public void UndoChild()
        {
            Children[LatestChildIndex].Undo();
            LatestChildIndex--;
            if (LatestChildIndex < 0)
                RaisePropertyChanged(nameof(CanUndoChild));
            RaisePropertyChanged(nameof(CanRedoChild));
        }

        /// <summary>
        /// Is there any child action to redo?
        /// </summary>
        public bool CanRedoChild => children != null && children.Count - 1 > latestChildIndex;

        public void RedoChild()
        {
            Children[LatestChildIndex + 1].Redo();
            LatestChildIndex++;
            if (!CanRedoChild)
                RaisePropertyChanged(nameof(CanRedoChild));
        }



        /// <summary>
        /// Called the first time the action is performed, if not merged. <seealso cref="Merge(UndoableAction)"/>
        /// </summary>
        public virtual void Do()
        {
            Redo();
        }

        /// <summary>
        /// Called when the action is to be undone.
        /// </summary>
        public void Undo()
        {
            UndoBeforeChildren();

            if (children != null)
                for (int i = children.Count - 1; i >= 0; i--)
                    children[i].Undo();

            UndoAfterChildren();
        }

        /// <summary>
        /// Called when the action is to be undone, before any child actions are undone.
        /// </summary>
        public virtual void UndoBeforeChildren() { }

        /// <summary>
        /// Called when the action is to be undone, after any child actions are undone.
        /// </summary>
        public virtual void UndoAfterChildren() { }

        /// <summary>
        /// Called when the action is to be redone.
        /// </summary>
        public void Redo()
        {
            RedoBeforeChildren();

            if (children != null)
                for (int i = 0; i < children.Count; i++)
                    children[i].Redo();

            RedoAfterChildren();
        }

        /// <summary>
        /// Called when the action is to be redone, before any child actions are redone.
        /// </summary>
        public virtual void RedoBeforeChildren() { }

        /// <summary>
        /// Called when the action is to be redone, after any child actions are redone.
        /// </summary>
        public virtual void RedoAfterChildren() { }

        /// <summary>
        /// When a new action is recorded, the previous one is given the opportunity to incorporate the new one.
        /// For example when the user is continuously changing a value, but we want to record just the final value.
        /// If the new action is merged into the existing one <see cref="Do"/> will not be called and it will not be recorded.
        /// </summary>
        /// <param name="action">the new action</param>
        /// <returns>true if the effects have been merged and the new action should not be recorded and performed</returns>
        /// <remarks>
        /// You will most likely want to copy the time from the merged action, or dragging an object will create new actions
        /// every <see cref="UndoManager.MergeTimeLimit"/>.
        /// </remarks>
        public virtual bool Merge(UndoableAction action) => false;
    }
}
