﻿using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public partial class MachineReportViewModel : BaseViewModel, IDisposable
	{
		private bool isDisposed;

		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		public MachineReportViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out MachineReportNavigationArgs? args))
			{
				this.TokenReport = args.Report;

				if (this.TokenReport is null)
					this.Title = string.Empty;
				else
				{
					this.Title = await this.TokenReport.GetTitle();
					await this.TokenReport.GenerateReport(this);
				}
			}

			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;

			this.DeleteTemporaryFiles();

			return base.OnDispose();
		}

		private Task Wallet_StateUpdated(object? Sender, NeuroFeatures.NewStateEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task Wallet_VariablesUpdated(object? Sender, NeuroFeatures.VariablesUpdatedEventArgs e)
		{
			return this.UpdateReport();
		}

		private Task UpdateReport()
		{
			return this.TokenReport?.GenerateReport(this) ?? Task.CompletedTask;
		}

		#region Properties

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private object? report;

		/// <summary>
		/// Parsed report from state-machine.
		/// </summary>
		[ObservableProperty]
		private TokenReport? tokenReport;


		/// <inheritdoc/>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		protected virtual void Dispose(bool Disposing)
		{
			if (this.isDisposed)
				return;

			if (Disposing)
				this.DeleteTemporaryFiles();

			this.isDisposed = true;
		}

		private void DeleteTemporaryFiles()
		{
			this.TokenReport?.DeleteTemporaryFiles();
		}

		#endregion

	}
}
