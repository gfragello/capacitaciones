using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Cursos.Helpers
{
    public class ValidationHelper
    {
        private static ValidationHelper instance = null;

        private ValidationHelper() { }

        public static ValidationHelper GetInstance()
        {
            if (instance == null)
                instance = new ValidationHelper();

            return instance;
        }

        public bool ValidateCI(string numeroCI)
        {
            try
            {
                //Control inicial sobre la cantidad de números ingresados. 
                if (numeroCI.Length == 8 || numeroCI.Length == 7)
                {
                    int[] _formula = { 2, 9, 8, 7, 6, 3, 4 };
                    int _suma = 0;
                    int _guion = 0;
                    int _aux = 0;
                    int[] _numero = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };

                    if (numeroCI.Length == 8)
                    {
                        _numero[0] = Convert.ToInt32(numeroCI[0].ToString());
                        _numero[1] = Convert.ToInt32(numeroCI[1].ToString());
                        _numero[2] = Convert.ToInt32(numeroCI[2].ToString());
                        _numero[3] = Convert.ToInt32(numeroCI[3].ToString());
                        _numero[4] = Convert.ToInt32(numeroCI[4].ToString());
                        _numero[5] = Convert.ToInt32(numeroCI[5].ToString());
                        _numero[6] = Convert.ToInt32(numeroCI[6].ToString());
                        _numero[7] = Convert.ToInt32(numeroCI[7].ToString());
                    }
                    //Para cédulas menores a un millón. 
                    else if (numeroCI.Length == 7)
                    {
                        _numero[0] = 0;
                        _numero[1] = Convert.ToInt32(numeroCI[0].ToString());
                        _numero[2] = Convert.ToInt32(numeroCI[1].ToString());
                        _numero[3] = Convert.ToInt32(numeroCI[2].ToString());
                        _numero[4] = Convert.ToInt32(numeroCI[3].ToString());
                        _numero[5] = Convert.ToInt32(numeroCI[4].ToString());
                        _numero[6] = Convert.ToInt32(numeroCI[5].ToString());
                        _numero[7] = Convert.ToInt32(numeroCI[6].ToString());
                    }

                    _suma = (_numero[0] * _formula[0]) + (_numero[1] * _formula[1]) + (_numero[2] * _formula[2]) + (_numero[3] * _formula[3]) + (_numero[4] * _formula[4]) + (_numero[5] * _formula[5]) + (_numero[6] * _formula[6]);

                    for (int i = 0; i < 10; i++)
                    {
                        _aux = _suma + i;
                        if (_aux % 10 == 0)
                        {
                            _guion = _aux - _suma;
                            i = 10;
                        }
                    }

                    if (_numero[7] == _guion)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string RemoveGuionCI(string ciConGuion)
        {
            if (ciConGuion.Contains("-"))
            {
                var pGuion = ciConGuion.IndexOf("-");
                return ciConGuion.Remove(pGuion, 1);
            }

            return ciConGuion;
        }

        public bool ValidateMultipleEmails(string multipleEmails)
        {
            Regex rgx = new Regex(@"^(([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)(\s*,\s*|\s*$))*$");
            return rgx.IsMatch(multipleEmails);
        }

        public bool EsURL(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult) && 
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}