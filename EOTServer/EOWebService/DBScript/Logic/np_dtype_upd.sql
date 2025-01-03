
-- iff > 0 #v_dtype_id
	-- set
	update n_dtype set 
		f_dtype = '#v_dtype',
        f_note = '#v_note'
		where f_dtype_id = #v_dtype_id;
	-- end
-- end
-- iff <= 0 #v_dtype_id
	-- inc #v_dtype_id
	insert into n_dtype(
		f_dtype,
        f_note, 
		_update_time, _update_flag) 
		values (
        '#v_dtype',
        '#v_note',
        ##now, 1);
	-- end
-- end

-- set
select 0 as _d, '' as _s, n_dtype.* from n_dtype where f_type_id = #v_type_id;
-- end