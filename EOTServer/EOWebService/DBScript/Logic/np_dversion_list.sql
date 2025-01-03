-- iff > 0 #v_dtype_id
    -- set
	select 0 as _d, '' as _s, n_dversion.*, n_dtype.f_dtype
		from n_dversion, n_dtype 
        where n_dversion._update_flag>0 
        and n_dversion.f_dtype_id = #v_dtype_id 
        and n_dversion.f_dtype_id = n_dtype.f_dtype_id
        order by f_dversion;
    -- end
-- end
-- iff <> '' #v_dversion_ids
    -- set
	select 0 as _d, '' as _s, n_dversion.*, n_dtype.f_dtype
		from n_dversion, n_dtype 
        where n_dversion._update_flag>0 
        and n_dversion.f_dtype_id = n_dtype.f_dtype_id
        and n_dversion.f_dversion_id in (#v_dversion_ids);
    -- end
-- end
