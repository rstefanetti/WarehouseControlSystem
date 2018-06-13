﻿// ----------------------------------------------------------------------------------
// Copyright © 2018, Oleg Lobakov, Contacts: <oleg.lobakov@gmail.com>
// Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// https://github.com/OlegLobakov/WarehouseControlSystem/blob/master/LICENSE
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarehouseControlSystem.ViewModel.Base;
using WarehouseControlSystem.Model.NAV;
using WarehouseControlSystem.Model;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using Plugin.Connectivity;
using WarehouseControlSystem.Helpers.NAV;
using WarehouseControlSystem.Resx;
using WarehouseControlSystem.View.Pages.RackScheme;
using System.Windows.Input;
using System.Threading;

namespace WarehouseControlSystem.ViewModel
{
    public class RacksViewModel : PlanBaseViewModel
    {
        public Zone Zone { get; set; }

        public RackViewModel SelectedRackViewModel { get; set; }
        public ObservableCollection<RackViewModel> RackViewModels { get; set; }
        public ObservableCollection<RackViewModel> SelectedViewModels { get; set; }

        public bool IsEditMode
        {
            get { return iseditmode; }
            set
            {
                if (iseditmode != value)
                {
                    iseditmode = value;
                    OnPropertyChanged("IsEditMode");
                }
            }
        } bool iseditmode;

        public ICommand RackListCommand { protected set; get; }
        public ICommand NewRackCommand { protected set; get; }
        public ICommand EditRackCommand { protected set; get; }
        public ICommand DeleteRackCommand { protected set; get; }

        public bool IsSelectedList { get { return SelectedViewModels.Count > 0; } }

        public ObservableCollection<UserDefinedSelectionViewModel> UserDefinedSelectionViewModels { get; set; }
        public bool IsVisibleUDS
        {
            get { return isvisibleUDS; }
            set
            {
                if (isvisibleUDS != value)
                {
                    isvisibleUDS = value;
                    OnPropertyChanged(nameof(IsVisibleUDS));
                }
            }
        } bool isvisibleUDS;
        public int UDSPanelHeight
        {
            get { return udspanelheight; }
            set
            {
                if (udspanelheight != value)
                {
                    udspanelheight = value;
                    OnPropertyChanged(nameof(UDSPanelHeight));
                }
            }
        } int udspanelheight;

        public RacksViewModel(INavigation navigation, Zone zone) : base(navigation)
        {
            Zone = zone;
            RackViewModels = new ObservableCollection<RackViewModel>();
            SelectedViewModels = new ObservableCollection<RackViewModel>();
            UserDefinedSelectionViewModels = new ObservableCollection<UserDefinedSelectionViewModel>();

            RackListCommand = new Command(RackList);
            NewRackCommand = new Command(NewRack);
            EditRackCommand = new Command(EditRack);
            DeleteRackCommand = new Command(DeleteRack);

            PlanWidth = zone.PlanWidth;
            PlanHeight = zone.PlanHeight;

            if (PlanHeight == 0)
            {
                PlanHeight = Settings.DefaultZonePlanHeight;
            }

            if (PlanWidth == 0)
            {
                PlanWidth = Settings.DefaultZonePlanWidth;
            }

            State = ModelState.Loading;
            IsEditMode = false;

        }

        public void ClearAll()
        {
            foreach (RackViewModel lvm in RackViewModels)
            {
                lvm.DisposeModel();
            }
            RackViewModels.Clear();
            SelectedViewModels.Clear();
            SelectedRackViewModel = null;
        }

        public async void Load()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                State = ModelState.Loading;
                List<Rack> Racks = await NAV.GetRackList(Zone.LocationCode, Zone.Code, true, 1, int.MaxValue, ACD.Default);
                if ((!IsDisposed) && (Racks is List<Rack>))
                {
                    if (Racks.Count > 0)
                    {
                        RackViewModels.Clear();
                        SelectedViewModels.Clear();
                        State = ModelState.Normal;
                        foreach (Rack rack in Racks)
                        {
                            RackViewModel rvm = new RackViewModel(Navigation, rack, false);
                            rvm.OnTap += Rvm_OnTap;
                            RackViewModels.Add(rvm);
                        }
                        ReDesign();
                        UpdateMinSizes();
                    }
                    else
                    {
                        State = ModelState.Error;
                        ErrorText = "No Data";
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                State = ModelState.Error;
                ErrorText = AppResources.Error_LoadRacks;
            }

        }

        public async void LoadAll()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                State = ModelState.Loading;
                List<Rack> racks = await NAV.GetRackList(Zone.LocationCode, Zone.Code, false, 1, int.MaxValue, ACD.Default);
                if ((!IsDisposed) && (racks is List<Rack>))
                {
                    if (racks.Count > 0)
                    {
                        State = ModelState.Normal;
                        foreach (Rack rack in racks)
                        {
                            RackViewModel rvm = new RackViewModel(Navigation, rack, false);
                            RackViewModels.Add(rvm);
                        }
                    }
                    else
                    {
                        State = ModelState.Error;
                        ErrorText = "No Data";
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                State = ModelState.Error;
                ErrorText = AppResources.Error_LoadRacksList;
            }

        }

        public async void LoadUDS()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                UserDefinedSelectionViewModels.Clear();
                List<UserDefinedSelection> list = await NAV.LoadUDS(Zone.LocationCode, Zone.Code, ACD.Default);
                if (list is List<UserDefinedSelection>)
                {
                    foreach (UserDefinedSelection uds in list)
                    {
                        UserDefinedSelectionViewModel udsvm = new UserDefinedSelectionViewModel(Navigation, uds)
                        {
                            UDSWidth = UDSPanelHeight,
                        };
                        udsvm.OnTap += RunUDS;

                        UserDefinedSelectionViewModels.Add(udsvm);
                    }
                }
                MessagingCenter.Send(this, "UDSListIsLoaded");
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                State = ModelState.Error;
                ErrorText = e.ToString();
            }

        }

        private async void Rvm_OnTap(RackViewModel rvm)
        {
            if (!IsEditMode)
            {
                await Navigation.PushAsync(new RackCardPage(rvm));
            }
            else
            {
                foreach (RackViewModel rv in RackViewModels)
                {
                    if (rv != rvm)
                    {
                        rv.Selected = false;
                    }
                }
                rvm.Selected = !rvm.Selected;

                SelectedViewModels = new ObservableCollection<RackViewModel>(RackViewModels.ToList().FindAll(x => x.Selected == true));
            }
        }

        private async void RunUDS(UserDefinedSelectionViewModel udsvm)
        {
            if (NotNetOrConnection)
            {
                return;
            }

            udsvm.State = ModelState.Loading;
            LoadAnimation = true;
            try
            {
                List<UserDefinedSelectionResult> list = await NAV.RunUDS(Zone.LocationCode, Zone.Code, udsvm.ID, ACD.Default);
                if (list is List<UserDefinedSelectionResult>)
                {
                    foreach (RackViewModel rvm in RackViewModels)
                    {
                        rvm.UDSSelects.RemoveAll(x => x.FunctionID == udsvm.ID);
                    }

                    foreach (UserDefinedSelectionResult udsr in list)
                    {
                        RackViewModel rvm = RackViewModels.ToList().Find(x => x.No == udsr.RackNo);
                        if (rvm is RackViewModel)
                        {
                            SubSchemeSelect sss = new SubSchemeSelect()
                            {
                                FunctionID = udsr.FunctionID,
                                Section = udsr.Section,
                                Level = udsr.Level,
                                Depth = udsr.Depth,
                                Value = udsr.Value,
                                HexColor = udsr.HexColor
                            };
                            rvm.UDSSelects.Add(sss);
                        }
                    }
                }
                MessagingCenter.Send(this, "UDSRunIsDone");
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
                LoadAnimation = false;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            udsvm.State = ModelState.Normal;

            LoadAnimation = false;
        }

        public override void ReDesign()
        {
            double widthstep = (ScreenWidth / PlanWidth);
            double heightstep = (ScreenHeight / PlanHeight);
            foreach (RackViewModel rvm in RackViewModels)
            {
                rvm.Left = rvm.Rack.Left * widthstep;
                rvm.Top = rvm.Rack.Top * heightstep;
                rvm.Width = rvm.Rack.Width * widthstep;
                rvm.Height = rvm.Rack.Height * heightstep;
            }
            MessagingCenter.Send(this, "Rebuild");
        }

        public void UnSelectAll()
        {
            foreach (RackViewModel rvm in RackViewModels)
            {
                rvm.Selected = false;
            }
            SelectedViewModels.Clear();
        }

        public async void RackList()
        {
            RackListPage rlp = new RackListPage(Zone);
            await Navigation.PushAsync(rlp);
        }

        public async void NewRack()
        {
            Rack newrack = new Rack()
            {
                Sections = Settings.DefaultRackSections,
                Levels = Settings.DefaultRackLevels,
                Depth = Settings.DefaultRackDepth,
                SchemeVisible = true,
            };
            RackViewModel rvm = new RackViewModel(Navigation, newrack, true)
            {
                LocationCode = Zone.LocationCode,
                ZoneCode = Zone.Code,
                CanChangeLocationAndZone = false
            };
            RackNewPage rnp = new RackNewPage(rvm);
            await Navigation.PushAsync(rnp);
        }

        public void EditRack(object obj)
        {
            System.Diagnostics.Debug.WriteLine(obj.ToString());
        }

        public void DeleteRack(object obj)
        {
            System.Diagnostics.Debug.WriteLine(obj.ToString());
        }

        public async void SaveZoneParams()
        {
            await NAV.ModifyZone(Zone, ACD.Default);
        }

        public async void SaveRacksChangesAsync()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            List<RackViewModel> list = RackViewModels.ToList().FindAll(x => x.Selected == true);
            foreach (RackViewModel rvm in list)
            {
                try
                {
                    await NAV.ModifyRack(rvm.Rack, ACD.Default);
                }
                catch
                {
                }
            }

        }

        public Task<string> SaveRacksVisible()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<string>();
            string rv = "";
            Task.Run(async () =>
            {
                try
                {
                    List<RackViewModel> list = RackViewModels.ToList().FindAll(x => x.Changed == true);
                    foreach (RackViewModel rvm in list)
                    {
                        Rack rack = new Rack();
                        rvm.SaveFields(rack);
                        await NAV.SetRackVisible(rack, cts).ConfigureAwait(false);
                    }
                    tcs.SetResult(rv);
                }
                catch
                {
                    tcs.SetResult(rv);
                }
            });
            return tcs.Task;
        }

        public void UpdateMinSizes()
        {
            foreach (RackViewModel rvm in RackViewModels)
            {
                if ((rvm.Rack.Left + rvm.Rack.Width) > MinPlanWidth)
                {
                    MinPlanWidth = rvm.Rack.Left + rvm.Rack.Width;
                }
                if ((rvm.Rack.Top + rvm.Rack.Height) > MinPlanHeight)
                {
                    MinPlanHeight = rvm.Rack.Top + rvm.Rack.Height;
                }
            }
        }

        public override void DisposeModel()
        {
            ClearAll();
            UserDefinedSelectionViewModels.Clear();
            base.DisposeModel();
        }
    }
}
