using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FakeSocket
{

    private FakeSocket m_pair = null;
    public FakeSocket pair {
        get
        {
            if (m_pair != null)
                return m_pair;
            else
            {
                m_pair = new FakeSocket();
                m_pair.pair = this;
                return m_pair;
            }

        }
        set
        {
            m_pair = value;
        }
    }

    private List<string> m_queue = new List<string>();

    public void Send(string data)
    {
        pair.AddQueue(data);
    }

    //returns a single element of the q
    public string Recieve()
    {
        if (m_queue.Count > 0)
        {
            //clear first item in queue
            string item = m_queue[0];
            m_queue.RemoveAt(0);
            return item;
        }
        else
        {
            //queue is empty
            return null;
        }
        
    }

    private void AddQueue(string data)
    {
        m_queue.Add(data);
    }
}
