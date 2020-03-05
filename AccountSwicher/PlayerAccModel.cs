namespace AccountSwicher
{
    public class PlayerAccModel
    {
        public string Name { get; set; }
        public string Note { get; set; }

        #region Registry

        public string IdName { get; set; }
        public string HashName { get; set; }
        public byte[] HashValue { get; set; }
        public byte[] IdValue { get; set; }

        #endregion
    }
}