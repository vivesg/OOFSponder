﻿using System;
using System.Collections.ObjectModel;

namespace OOFScheduling
{
    public class OOFData
    {
        internal DateTime PermaOOFDate { get; set; }

        private string _workingHours;
        internal string WorkingHours
        {
            get
            {
                return _workingHours;
            }
            set
            {
                _workingHours = value;
                //if we update WorkingHours, then blow away OOFCollection
                _OOFCollection = null;
            }
        }
        internal string PrimaryOOFExternalMessage { get; set; }
        internal string PrimaryOOFInternalMessage { get; set; }
        internal string SecondaryOOFExternalMessage { get; set; }
        internal string SecondaryOOFInternalMessage { get; set; }

        internal Collection<OOFInstance> _OOFCollection;
        internal Collection<OOFInstance> OOFCollection
        {
            get
            {
                if (_OOFCollection == null)
                {
                    _OOFCollection = new Collection<OOFInstance>();
                }

                if (_OOFCollection.Count != 7)
                {
                    //convert the array of string objects to real objects
                    string[] workingTimes = WorkingHours.Split('|');
                    for (int i = 0; i < 7; i++)
                    {
                        string[] currentWorkingTime = workingTimes[i].Split('~');
                        OOFInstance OOFItem = new OOFInstance();
                        OOFItem.dayOfWeek = (DayOfWeek)i;
                        OOFItem.StartTime = DateTime.Parse(currentWorkingTime[0]);
                        OOFItem.EndTime = DateTime.Parse(currentWorkingTime[1]);
                        if (currentWorkingTime[2] == "0")
                        {
                            OOFItem.IsOOF = false;
                        }
                        else
                        {
                            OOFItem.IsOOF = true;
                        }
                        OOFItem.isOnCallModeEnabled = this.IsOnCallModeOn;

                        _OOFCollection.Add(OOFItem);
                    }
                }

                return _OOFCollection;
            }
        }

        //Track whether or not to run in OnCallMode
        //When in this mode, the OOF times get flipped and instead of 
        //tracking days on/days off, they will track a start/end for OOF *during* the working day
        internal bool IsOnCallModeOn { get; set; }

        private const string baseValue = "default";
        internal static string version;
        static OOFData instance;

        public static OOFData Instance
        {
            get
            {

                if (instance == null)
                {
                    instance = new OOFData();
                    instance.ReadProperties();
                }
                return instance;
            }
        }

        internal OOFInstance currentOOFPeriod
        {
            get
            {
                return this.OOFCollection[(int)DateTime.Now.DayOfWeek];
            }
        }

        internal DateTime previousOOFPeriodEnd
        {
            get
            {
                string datePart=string.Empty;
                string timePart=string.Empty;
                DateTime _previousOOFPeriodEnd;

                //if normal OOF, previous OOFPeriod is the previous day
                //if onCallMode, then previous OOFPeriod is the last day with OOF on
                if (IsOnCallModeOn)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (this.OOFCollection[i].IsOOF)
                        {
                            datePart = this.OOFCollection[i].EndTime.ToShortDateString();
                            timePart = this.OOFCollection[i].EndTime.ToShortTimeString();
                        }
                    }
                }
                else
                {
                    datePart = DateTime.Now.AddDays(-1).ToShortDateString();
                    timePart = this.OOFCollection[(int)(DateTime.Now.AddDays(-1).DayOfWeek)].EndTime.ToShortTimeString();
                }

                _previousOOFPeriodEnd = DateTime.Parse(datePart + " " + timePart);
                return _previousOOFPeriodEnd;
            }
        }

        internal DateTime nextOOFPeriodStart
        {
            get
            {
                string datePart = DateTime.Now.AddDays(1).ToShortDateString();
                string timePart = this.OOFCollection[(int)(DateTime.Now.AddDays(1).DayOfWeek)].StartTime.ToShortTimeString();
                DateTime _nextOOFPeriodStart = DateTime.Parse(datePart + " " + timePart);
                return _nextOOFPeriodStart;
            }
        }

        internal DateTime nextOOFPeriodEnd
        {
            get
            {
                string datePart = DateTime.Now.AddDays(1).ToShortDateString();
                string timePart = this.OOFCollection[(int)(DateTime.Now.AddDays(1).DayOfWeek)].EndTime.ToShortTimeString();
                DateTime _nextOOFPeriodEnd = DateTime.Parse(datePart + " " + timePart);
                return _nextOOFPeriodEnd;
            }
        }

        internal bool IsPermaOOFOn
        {
            get
            {
                return DateTime.Now < PermaOOFDate;
            }
        }

        public bool useNewOOFMath { get; internal set; }

        private void ReadProperties()
        {
            OOFSponder.Logger.Info("Reading settings");

            instance.PermaOOFDate = OOFScheduling.Properties.Settings.Default.PermaOOFDate;
            instance.WorkingHours = OOFScheduling.Properties.Settings.Default.workingHours == baseValue ? string.Empty : Properties.Settings.Default.workingHours;
            instance.PrimaryOOFExternalMessage = OOFScheduling.Properties.Settings.Default.PrimaryOOFExternal == baseValue ? string.Empty : Properties.Settings.Default.PrimaryOOFExternal;
            instance.PrimaryOOFInternalMessage = OOFScheduling.Properties.Settings.Default.PrimaryOOFInternal == baseValue ? string.Empty : Properties.Settings.Default.PrimaryOOFInternal;
            instance.SecondaryOOFExternalMessage = OOFScheduling.Properties.Settings.Default.SecondaryOOFExternal == baseValue ? string.Empty : Properties.Settings.Default.SecondaryOOFExternal;
            instance.SecondaryOOFInternalMessage = OOFScheduling.Properties.Settings.Default.SecondaryOOFInternal == baseValue ? string.Empty : Properties.Settings.Default.SecondaryOOFInternal;
            instance.IsOnCallModeOn = OOFScheduling.Properties.Settings.Default.enableOnCallMode;

            OOFSponder.Logger.Info("Successfully read settings");
        }

        ~OOFData()
        {
            Dispose(false);
        }

        public void WriteProperties(bool disposing = false)
        {
            OOFSponder.Logger.Info("Persisting settings");

            Properties.Settings.Default.PrimaryOOFExternal = instance.PrimaryOOFExternalMessage;
            OOFSponder.Logger.Info("Persisted PrimaryOOFExternalMessage");

            Properties.Settings.Default.PrimaryOOFInternal = instance.PrimaryOOFInternalMessage;
            OOFSponder.Logger.Info("Persisted PrimaryOOFInternalMessage");

            Properties.Settings.Default.SecondaryOOFExternal = instance.SecondaryOOFExternalMessage;
            OOFSponder.Logger.Info("Persisted SecondaryOOFExternalMessage");

            Properties.Settings.Default.SecondaryOOFInternal = instance.SecondaryOOFInternalMessage;
            OOFSponder.Logger.Info("Persisted SecondaryOOFInternalMessage");

            Properties.Settings.Default.PermaOOFDate = instance.PermaOOFDate;
            OOFSponder.Logger.Info("Persisted PermaOOFDate");

            Properties.Settings.Default.workingHours = instance.WorkingHours;
            OOFSponder.Logger.Info("Persisted WorkingHours");

            Properties.Settings.Default.enableOnCallMode = instance.IsOnCallModeOn;
            OOFSponder.Logger.Info("Persisted enableOnCallMode = " + instance.IsOnCallModeOn.ToString());

            Properties.Settings.Default.Save();
            OOFSponder.Logger.Info("Persisted settings");

            if (disposing)
            {
                Dispose();
            }

        }

        #region IDisposable Members

        public void Dispose()
        {

            Dispose(true);

            // Turn off calling the finalizer
            System.GC.SuppressFinalize(this);

        }

        #endregion

        public void Dispose(bool disposing)
        {
            // Do not dispose of an owned managed object (one with a
            // finalizer) if called by member finalize,
            // as the owned managed objects finalize method
            // will be (or has been) called by finalization queue
            // processing already
            WriteProperties(disposing);
        }
    }
    public class OOFInstance
    {
        private DateTime _startTime;
        private DateTime _endTime;
        internal DayOfWeek dayOfWeek;
        internal bool isOnCallModeEnabled = false;

        private bool _isOOF;
        public bool IsOOF
        {
            get
            {
                if (isOnCallModeEnabled)
                {
                    return !_isOOF;
                }
                else
                {
                    return _isOOF;
                }
            }

            set => _isOOF = value;
        }

        internal DateTime StartTime
        {
            get
            {
                //need to return the *actual* day and not just the day of week
                return _startTime.EquivalentDateTime(this.dayOfWeek);
            }
            set => _startTime = value;
        }
        internal DateTime EndTime
        {
            get
            {
                //need to return the *actual* day and not just the day of week
                return _endTime.EquivalentDateTime(this.dayOfWeek);
            }
            set => _endTime = value;
        }
    }
    public static class DateTimeExtensions
    {
        //figures out the actual day from a generic day of the week
        public static DateTime EquivalentDateTime(this DateTime dtOld, DayOfWeek dayOfWeek)
        {
            int referenceDayofWeek = (int)dayOfWeek;
            int todayDayofWeek = (int)DateTime.Today.DayOfWeek;
            int deltaDayofWeek = referenceDayofWeek - toda;
            if (deltaDayofWeek <0) {deltaDayofWeek += 7;} //if the day of the week is *before* today, then we are really talking about next week
            DateTime _localDT = DateTime.Today.AddDays(deltaDayofWeek).AddHours(dtOld.Hour).AddMinutes(dtOld.Minute);
            return _localDT;
        }
    }
}
