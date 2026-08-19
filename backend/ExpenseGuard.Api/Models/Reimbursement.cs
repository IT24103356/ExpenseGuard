public class Reimbursement{
    public int reimb_id { get; set; }
    public int claim_id { get; set; }
    public decimal total { get; set; }
    public varchar status { get; set; }
    public DateTime processed_at { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public DateTime deleted_at { get; set; }
}