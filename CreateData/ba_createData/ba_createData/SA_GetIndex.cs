using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ba_createData
{
    [Serializable]
    public class SA_GetIndex
    {
        public int IndexOf(string substr, int[] m_sa, string m_str )
        {
            int l = 0;
            int r = m_sa.Length;
            int m = -1;

            if ((substr == null) || (substr.Length == 0))
            {
                return -1;
            }

            // Binary search for substring
            while (r > l)
            {
                m = (l + r) / 2;
                if (m_str.Substring(m_sa[m]).CompareTo(substr) < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }
            if ((l == r) && (l < m_str.Length) && (m_str.Substring(m_sa[l]).StartsWith(substr)))
            {
                return m_sa[l];
            }
            else
            {
                return -1;
            }
        }
    }
}
