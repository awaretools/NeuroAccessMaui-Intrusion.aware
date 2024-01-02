﻿using Mopups.Services;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Popups
{
	public partial class SelectPhoneCodePage : IDisposable
	{
		private readonly TaskCompletionSource<ISO3166Country?> result = new();
		private CancellationTokenSource? cancellationTokenSource;
		private bool isDisposed;

		/// <summary>
		/// Task waiting for result. null means dialog was closed without selection.
		/// </summary>
		public Task<ISO3166Country?> Result => this.result.Task;

		public List<ISO3166Country> Countries { get; } = new(ISO_3166_1.Countries);

		public SelectPhoneCodePage(ImageSource? Background = null)
			: base(Background)
		{
			this.InitializeComponent();
			this.BindingContext = this;

			this.InnerSearchBar.Text = string.Empty;
		}

		private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (e.NewTextValue.Length == 0)
			{
				this.InnerListView.ItemsSource = this.Countries;
				return;
			}

			if (this.cancellationTokenSource is not null)
			{
				this.cancellationTokenSource.Cancel();
				this.cancellationTokenSource = null;
			}

			this.cancellationTokenSource = new();
			CancellationToken Token = this.cancellationTokenSource.Token;

			Task.Run(() =>
			{
				IEnumerable<ISO3166Country> CountriesFiltered = this.Countries.Where(el =>
				{
					bool Result = el.Name.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(el.Alpha2, e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(el.Alpha3, e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
					el.DialCode.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase);

					return Result;
				});

				this.Dispatcher.Dispatch(() =>
				{
					if (!Token.IsCancellationRequested)
						this.InnerListView.ItemsSource = CountriesFiltered;
				});
			}, Token);
		}

		private async void InnerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.result.TrySetResult((ISO3166Country)this.InnerListView.SelectedItem);
			await MopupService.Instance.PopAsync();
		}

		protected override void OnDisappearing()
		{
			this.result.TrySetResult(null);
			base.OnDisappearing();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.isDisposed)
			{
				if (disposing)
				{
					this.cancellationTokenSource?.Dispose();
					this.cancellationTokenSource = null;
				}

				this.Countries.Clear();

				this.isDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
