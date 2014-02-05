using System;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public abstract class ProcessorViewModelBase<T> : ViewModelBase
    {
        #region commands

        #region Drag & Drop commands

        public RelayCommand<DragEventArgs> DragOverCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropTargetCommand { get; private set; }

        #endregion

        public RelayCommand<T> RemoveCommand { get; private set; }

        #endregion

        #region ctor

        internal ProcessorViewModelBase()
        {
            #region Drag & Drop

            DragOverCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetFormats().Any(f => Equals(typeof(T).Name, f)))
                    {
                        e.Effects = DragDropEffects.None;
                    }
                });

            DropTargetCommand = new RelayCommand<DragEventArgs>(e =>
            {
                if (!e.Data.GetFormats().Any(f => Equals(typeof(T).Name, f))) return;
                var type = e.Data.GetData(typeof(T).Name) as Type;

                if (type == null)
                    return;

                T t;
                try
                {
                    t = (T)Activator.CreateInstance(type);
                }
                catch (Exception)
                {
                    t = default(T);
                }
                OnAdd(t);
            });

            #endregion

            RemoveCommand = new RelayCommand<T>(OnRemove);
        }

        #endregion

        protected abstract void OnAdd(T t);

        protected abstract void OnRemove(T t);
    }
}
