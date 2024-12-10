-- iff > 0 #v_type_id
    -- set
	select 0 as _d, '' as _s, n_dversion.*, n_dtype.f_dtype
		from n_dversion, n_dtype 
        where n_dversion._update_flag>0 
        and n_dversion.f_type_id = #v_type_id 
        and n_dversion.f_type_id = n_dtype.f_type_id
        order by f_dversion;
    -- end
-- end
-- iff <= 0 #v_type_id
    -- set
	select 0 as _d, '' as _s, n_dversion.*, n_dtype.f_dtype
		from n_dversion, n_dtype 
        where n_dversion._update_flag>0 
        and n_dversion.f_type_id = n_dtype.f_type_id
        order by f_dversion;
    -- end
-- end
