namespace PhoneBookProject
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class PhoneBook
    {
        private const string PhoneBookFileName = @"..\..\PhoneBook.txt";

        private Dictionary<string, string> phoneBookList = new Dictionary<string, string>();

        public void ReadPhoneBook()
        {
            StreamReader reader = new StreamReader(PhoneBookFileName, Encoding.GetEncoding("windows-1251"));
            using (reader)
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    string[] entry = line.Split(new char[] { ' ', ',' });
                    string name = entry[0].Trim();
                    string number = entry[1].Trim();

                    ///...

                }
            }
        }

        public void AddToPhoneBook(string name, string number)
        {
            name = name.ToLower();
            List<string> numbers = new List<string>();

            if (!this.phoneBookList.TryGetValue(name, out number))
            {
                this.phoneBookList.Add(name, number);
            }

            numbers.Add(number);
        }

        public void DeleteFromPhoneBook(string name, string number)
        {
            if (this.phoneBookList.ContainsKey(name))
            {
                this.phoneBookList.Remove(name);
            }
        }

        public void FindNumberByName(string name, string number)
        {
            if (this.phoneBookList.TryGetValue(name, out number))
            {
                Console.WriteLine("{0}'s number is {1}", name, number);
            }
            else
            {
                Console.WriteLine("This name is not found!");
            }
        }

        public void PrintAllEntries()
        {
            var phoneBookList = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in phoneBookList)
            {
                Console.WriteLine("{0} - {1}", entry.Key, entry.Value);
            }
        }

        // Regex for mobile phones
        //  \359[7-9]{2}[2-9]{1}[0-9]{6}
        //  08[7-9][0-9]{7}/
    }
}
