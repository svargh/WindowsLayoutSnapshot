using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.ToolStripItem;

namespace WindowsLayoutSnapshot
{

    public partial class TrayIconForm : Form
    {

        private List<Snapshot> m_snapshots = new List<Snapshot>();
        private Snapshot m_menuShownSnapshot = null;
        private Padding? m_originalTrayMenuArrowPadding = null;
        private Padding? m_originalTrayMenuTextPadding = null;

        internal static ContextMenuStrip me { get; set; }

        public TrayIconForm(bool use2ndIcon)
        {
            InitializeComponent();
            Visible = false;
            me = trayMenu;
            TakeSnapshot(false);

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrayIconForm));
            if (use2ndIcon) {
                this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon2")));
            } else {
                this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon1")));
            }
            
        }

        private void snapshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TakeSnapshot(true);
        }

        private void TakeSnapshot(bool userInitiated)
        {
            m_snapshots.Add(Snapshot.TakeSnapshot(userInitiated));
            UpdateRestoreChoicesInMenu();
        }

        private void clearSnapshotsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_snapshots.Clear();
            UpdateRestoreChoicesInMenu();
        }

        private void justNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_menuShownSnapshot.Restore(null, EventArgs.Empty);
        }

        private void justNowToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            SnapshotMousedOver(sender, e);
        }

        private class RightImageToolStripMenuItem : ToolStripMenuItem
        {
            public RightImageToolStripMenuItem(string text)
                : base(text)
            {
            }
            public float[] MonitorSizes { get; set; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var icon = global::WindowsLayoutSnapshot.Properties.Resources.monitor;
                var maxIconSizeScaling = ((float)(e.ClipRectangle.Height - 8)) / icon.Height;
                var maxIconSize = new Size((int)Math.Floor(icon.Width * maxIconSizeScaling), (int)Math.Floor(icon.Height * maxIconSizeScaling));
                int maxIconY = (int)Math.Round((e.ClipRectangle.Height - maxIconSize.Height) / 2f);

                int nextRight = e.ClipRectangle.Width - 5;
                for (int i = 0; i < MonitorSizes.Length; i++)
                {
                    var thisIconSize = new Size((int)Math.Ceiling(maxIconSize.Width * MonitorSizes[i]),
                        (int)Math.Ceiling(maxIconSize.Height * MonitorSizes[i]));
                    var thisIconLocation = new Point(nextRight - thisIconSize.Width,
                        maxIconY + (maxIconSize.Height - thisIconSize.Height));

                    // Draw with transparency
                    var cm = new ColorMatrix();
                    cm.Matrix33 = 0.7f; // opacity
                    using (var ia = new ImageAttributes())
                    {
                        ia.SetColorMatrix(cm);

                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(icon, new Rectangle(thisIconLocation, thisIconSize), 0, 0, icon.Width,
                            icon.Height, GraphicsUnit.Pixel, ia);
                    }

                    nextRight -= thisIconSize.Width + 4;
                }
            }
        }

        //MenuItem gets removed if Shift is pressed
        private void RestoreOrRemoveMenuItem(object sender, EventArgs e)
        {
            ToolStripMenuItem mnu = (ToolStripMenuItem)sender;
            Snapshot ss = (Snapshot)mnu.Tag;
            if (Control.ModifierKeys == Keys.Shift)
            {
                m_snapshots.Remove(ss);
                trayMenu.Items.Remove(mnu);
            }
            else
            {
                ss.Restore(sender, e);
            }
        }

        private void UpdateRestoreChoicesInMenu()
        {
            // construct the new list of menu items, then populate them
            // this function is idempotent

            var snapshotsOldestFirst = new List<Snapshot>(CondenseSnapshots(m_snapshots, 20));
            var newMenuItems = new List<ToolStripItem>();

            newMenuItems.Add(quitToolStripMenuItem);
            newMenuItems.Add(snapshotListEndLine);

            var maxNumMonitors = 0;
            var maxNumMonitorPixels = 0L;
            var showMonitorIcons = false;
            foreach (var snapshot in snapshotsOldestFirst)
            {
                if (maxNumMonitors != snapshot.NumMonitors && maxNumMonitors != 0)
                {
                    showMonitorIcons = true;
                }

                maxNumMonitors = Math.Max(maxNumMonitors, snapshot.NumMonitors);
                foreach (var monitorPixels in snapshot.MonitorPixelCounts)
                {
                    maxNumMonitorPixels = Math.Max(maxNumMonitorPixels, monitorPixels);
                }
            }

            foreach (var snapshot in snapshotsOldestFirst)
            {
                var menuItem = new RightImageToolStripMenuItem(snapshot.TimeTaken.ToLocalTime().ToString("MMM dd, h:mm:ss"));
                menuItem.Tag = snapshot;
                menuItem.Click += RestoreOrRemoveMenuItem;
                menuItem.ToolTipText = "Press Shift and Right Mouse to remove selected snapshot.";
                if (snapshot.UserInitiated)
                {
                    menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
                }

                // monitor icons
                var monitorSizes = new List<float>();
                if (showMonitorIcons)
                {
                    foreach (var monitorPixels in snapshot.MonitorPixelCounts)
                    {
                        monitorSizes.Add((float)Math.Sqrt(((float)monitorPixels) / maxNumMonitorPixels));
                    }
                }
                menuItem.MonitorSizes = monitorSizes.ToArray();

                newMenuItems.Add(menuItem);
            }

            newMenuItems.Add(snapshotListStartLine);
            newMenuItems.Add(clearSnapshotsToolStripMenuItem);
            newMenuItems.Add(snapshotToolStripMenuItem);

            // if showing monitor icons: subtract 34 pixels from the right due to too much right padding
            try
            {
                var textPaddingField = typeof(ToolStripDropDownMenu).GetField("TextPadding", BindingFlags.NonPublic | BindingFlags.Static);
                if (!m_originalTrayMenuTextPadding.HasValue)
                {
                    m_originalTrayMenuTextPadding = (Padding)textPaddingField.GetValue(trayMenu);
                }
                textPaddingField.SetValue(trayMenu, new Padding(m_originalTrayMenuTextPadding.Value.Left, m_originalTrayMenuTextPadding.Value.Top,
                    m_originalTrayMenuTextPadding.Value.Right - (showMonitorIcons ? 34 : 0), m_originalTrayMenuTextPadding.Value.Bottom));
            }
            catch
            {
                // something went wrong with using reflection
                // there will be extra hanging off to the right but that's okay
            }

            // if showing monitor icons: make the menu item width 50 + 22 * maxNumMonitors pixels wider than without the icons, to make room 
            //   for the icons
            try
            {
                var arrowPaddingField = typeof(ToolStripDropDownMenu).GetField("ArrowPadding", BindingFlags.NonPublic | BindingFlags.Static);
                if (!m_originalTrayMenuArrowPadding.HasValue)
                {
                    m_originalTrayMenuArrowPadding = (Padding)arrowPaddingField.GetValue(trayMenu);
                }
                arrowPaddingField.SetValue(trayMenu, new Padding(m_originalTrayMenuArrowPadding.Value.Left, m_originalTrayMenuArrowPadding.Value.Top,
                    m_originalTrayMenuArrowPadding.Value.Right + (showMonitorIcons ? 50 + 22 * maxNumMonitors : 0),
                    m_originalTrayMenuArrowPadding.Value.Bottom));
            }
            catch
            {
                // something went wrong with using reflection
                if (showMonitorIcons)
                {
                    // add padding a hacky way
                    var toAppend = "      ";
                    for (int i = 0; i < maxNumMonitors; i++)
                    {
                        toAppend += "           ";
                    }
                    foreach (var menuItem in newMenuItems)
                    {
                        menuItem.Text += toAppend;
                    }
                }
            }

            trayMenu.Items.Clear();
            trayMenu.Items.AddRange(newMenuItems.ToArray());
        }

        private List<Snapshot> CondenseSnapshots(List<Snapshot> snapshots, int maxNumSnapshots)
        {
            if (maxNumSnapshots < 2)
            {
                throw new Exception();
            }

            // find maximally different snapshots
            // snapshots is ordered by time, ascending

            // todo:
            // consider these factors (in rough order of importance):
            //   * number of total desktop pixels in snapshot (i.e. different monitor configs like two displays vs laptop display only etc)
            //   * snapshot age
            //   * window states (maximized/minimized)
            //   * window positions

            // for now, a poor man's version:

            // remove automatically-taken snapshots > 3 days old, or manual snapshots > 5 days old
            var y = new List<Snapshot>();
            y.AddRange(snapshots);
            while (y.Count > maxNumSnapshots)
            {
                for (int i = 0; i < y.Count; i++)
                {
                    if (y[i].Age > TimeSpan.FromDays(y[i].UserInitiated ? 5 : 3))
                    {
                        y.RemoveAt(i);
                        continue;
                    }
                }
                break;
            }

            // remove entries with the time most adjacent to another time
            while (y.Count > maxNumSnapshots)
            {
                int ixMostAdjacentNeighbors = -1;
                TimeSpan lowestDistanceBetweenNeighbors = TimeSpan.MaxValue;
                for (int i = 1; i < y.Count - 1; i++)
                {
                    var distanceBetweenNeighbors = (y[i + 1].TimeTaken - y[i - 1].TimeTaken).Duration();

                    if (y[i].UserInitiated)
                    {
                        // a hack to make manual snapshots prioritized over automated snapshots
                        distanceBetweenNeighbors += TimeSpan.FromDays(1000000);
                    }
                    if (DateTime.UtcNow.Subtract(y[i].TimeTaken).Duration() <= TimeSpan.FromHours(2))
                    {
                        // a hack to make very recent snapshots prioritized over other snapshots
                        distanceBetweenNeighbors += TimeSpan.FromDays(2000000);
                    }

                    if (distanceBetweenNeighbors < lowestDistanceBetweenNeighbors)
                    {
                        lowestDistanceBetweenNeighbors = distanceBetweenNeighbors;
                        ixMostAdjacentNeighbors = i;
                    }
                }
                y.RemoveAt(ixMostAdjacentNeighbors);
            }

            return y;
        }

        private void SnapshotMousedOver(object sender, EventArgs e)
        {
            //Commented out, takes too much time to preview....
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            m_menuShownSnapshot = Snapshot.TakeSnapshot(false);

            // the context menu won't show by default on left clicks.  we're going to have to ask it to show up.
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    // try using reflection to get to the private ShowContextMenu() function...which really 
                    // should be public but is not.
                    var showContextMenuMethod = trayIcon.GetType().GetMethod("ShowContextMenu",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    showContextMenuMethod.Invoke(trayIcon, null);
                }
                catch (Exception)
                {
                    // something went wrong with out hack -- fall back to a shittier approach
                    trayMenu.Show(Cursor.Position);
                }
            }
        }

        private void TrayIconForm_VisibleChanged(object sender, EventArgs e)
        {
            // Application.Run(Form) changes this form to be visible.  Change it back.
            Visible = false;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
    }
}