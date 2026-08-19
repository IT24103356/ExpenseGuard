public class Budget
{
    public int budget_id { get; set; }
    public string dept_id { get; set; }
    public decimal allocated_amount { get; set; }
    public DateTime period { get; set; }
    public string spent_amount { get; set; }
}