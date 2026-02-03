using System;
using Microsoft.UI.Xaml;

namespace v2rayWinUI.UI.Xaml
{
    internal static class FrameworkElementExtensions
    {
        /// <summary>
        /// Initialize the DataContext for a view using the provided service provider.
        /// Falls back to creating an instance when the service is not available.
        /// </summary>
        public static void InitializeDataContext<TViewModel>(this FrameworkElement root, IServiceProvider? serviceProvider)
            where TViewModel : class, new()
        {
            if (root == null)
            {
                return;
            }

            object? vm = null;
            try
            {
                if (serviceProvider != null)
                {
                    vm = serviceProvider.GetService(typeof(TViewModel)) as TViewModel;
                }
            }
            catch
            {
                vm = null;
            }

            vm ??= Activator.CreateInstance<TViewModel>();

            root.DataContext = vm;
        }
    }
}
