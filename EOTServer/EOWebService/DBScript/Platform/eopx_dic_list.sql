
-- iff > 0 #v_dic_pid
	-- set
	select 0 as _d, '' as _s, eox_dic.* from eox_dic
		where _update_flag>0 and f_dic_pid=#v_dic_pid
        order by f_value;
	-- end
-- end

-- iff <= 0 #v_dic_pid
	-- set
	select 0 as _d, '' as _s, eox_dic.* from eox_dic
		where _update_flag>0
        order by f_dic_pid,f_value;
	-- end
-- end