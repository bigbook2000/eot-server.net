---- 获取部门数据字段清单
    
-- set
select * from n_data_field where f_dept_id=#v_dept_id and _update_flag>0

-- add <> '' #v_type
	and f_type = '#v_type'
-- end

order by f_order
-- end