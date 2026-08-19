public class ExpenseClaim
{
    public int claim_id { get; set; }
    public int emp_id { get; set; }
    public varchar catagory { get; set; }
    public decimal amount { get; set; }
    public DateTime date { get; set; }
    public varchar purchase_no { get; set; }
    public varchar status { get; set; }
    public text receipt_img { get; set; }
    public text receipt_doc {get; set;}
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public DateTime deleted_at { get; set; }
    public DateTime submitted_at { get; set; }
}